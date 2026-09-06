using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.AiScanner.DTOs;
using MinimalAPI.Application.Features.AiScanner.Interfaces;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.ValueObjects;
using MinimalAPI.Infrastructure.Persistence;

namespace MinimalAPI.Api.Endpoints;

public static class AiScannerEndpoints
{
    public static WebApplication MapAiScannerEndpoints(this WebApplication app)
    {
        var aiGroup = app.MapGroup("/api/ai")
            .WithTags("AI Invoice Scanner")
            .RequireAuthorization();

        // 1. Quét hóa đơn qua AI (Upload ảnh hoặc chụp từ Camera)
        aiGroup.MapPost("/scan", async (
            HttpRequest request,
            ICurrentStore currentStore,
            IAiVisionService aiService,
            CancellationToken ct) =>
        {
            var storeId = currentStore.GetRequiredStoreId();

            byte[] bytes;
            string contentType = "image/jpeg";
            string? sampleType = request.Query["sampleType"].FirstOrDefault();

            if (request.HasFormContentType)
            {
                if (request.Form.Files.Count > 0)
                {
                    var image = request.Form.Files[0];
                    using var ms = new MemoryStream();
                    await image.CopyToAsync(ms, ct);
                    bytes = ms.ToArray();
                    contentType = image.ContentType;
                }
                else
                {
                    bytes = Encoding.UTF8.GetBytes("sample-invoice-image");
                }

                if (string.IsNullOrWhiteSpace(sampleType) && request.Form.TryGetValue("sampleType", out var formSample))
                {
                    sampleType = formSample.FirstOrDefault();
                }
            }
            else
            {
                // Nếu không gửi file, sử dụng ảnh buffer mẫu mô phỏng
                bytes = Encoding.UTF8.GetBytes("sample-invoice-image");
            }

            var customApiKey = request.Headers["X-Gemini-Key"].FirstOrDefault();

            var result = await aiService.ExtractInvoiceFromImageAsync(
                bytes, 
                contentType, 
                storeId.Value, 
                customApiKey, 
                sampleType, 
                ct);

            return Results.Ok(result);
        }).DisableAntiforgery();

        // 2. Xác nhận đơn hàng đã quét & Nhập kho + Đồng bộ Google Sheets
        aiGroup.MapPost("/confirm", async (
            ConfirmScannedOrderRequest request,
            ICurrentStore currentStore,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var storeId = currentStore.GetRequiredStoreId();

            if (request.Items.Count == 0)
            {
                return Results.BadRequest(new { error = "Đơn hàng phải có ít nhất một sản phẩm." });
            }

            // 1. Tìm hoặc tạo khách hàng
            var cleanPhone = request.Phone?.Trim() ?? "0908889999";
            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.StoreId == storeId && c.Phone == cleanPhone, ct);

            if (customer is null)
            {
                customer = Customer.Create(
                    storeId,
                    string.IsNullOrWhiteSpace(request.CustomerName) ? "Khách Hàng AI" : request.CustomerName.Trim(),
                    cleanPhone,
                    null,
                    string.IsNullOrWhiteSpace(request.Address) ? "Hồ Chí Minh" : request.Address.Trim()
                );
                db.Customers.Add(customer);
                await db.SaveChangesAsync(ct);
            }

            // 2. Tạo Order Items & trừ kho
            var orderId = OrderId.New();
            var orderItems = new List<OrderItem>();

            foreach (var reqItem in request.Items)
            {
                var product = await db.Products
                    .FirstOrDefaultAsync(p => p.Id == new ProductId(reqItem.ProductId) && p.StoreId == storeId, ct);

                if (product is null)
                {
                    // Fallback nếu sản phẩm không tìm thấy: lấy sản phẩm đầu tiên của store
                    product = await db.Products.FirstOrDefaultAsync(p => p.StoreId == storeId, ct);
                }

                if (product is not null)
                {
                    // Trừ tồn kho
                    product.AdjustStock(-reqItem.Quantity);

                    orderItems.Add(OrderItem.Create(
                        orderId,
                        product.Id,
                        product.Name.Value,
                        Money.VND(reqItem.UnitPrice > 0 ? reqItem.UnitPrice : product.Price.Amount),
                        reqItem.Quantity > 0 ? reqItem.Quantity : 1
                    ));
                }
            }

            if (orderItems.Count == 0)
            {
                return Results.BadRequest(new { error = "Không thể tìm thấy sản phẩm hợp lệ trong kho để tạo đơn." });
            }

            var orderCode = $"ORD-AI-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var order = Order.Create(storeId, customer.Id, orderCode, orderItems);
            order.Confirm(); // Xác nhận đơn ngay để ghi nhận doanh thu & tồn kho

            db.Orders.Add(order);

            // 3. Tạo Outbox Message đồng bộ tự động sang Google Sheets
            var outboxPayload = JsonSerializer.Serialize(new
            {
                orderId = order.Id.Value,
                orderCode = order.Code,
                customerName = customer.Name,
                customerPhone = customer.Phone,
                customerAddress = customer.Address,
                totalAmount = order.TotalAmount.Amount,
                currency = "VND",
                itemsCount = order.Items.Count,
                source = "AI_Vision_Scanner",
                status = "Confirmed",
                createdAt = DateTime.UtcNow
            });

            var outbox = OutboxMessage.Create(storeId, "OrderCreated", outboxPayload);
            db.OutboxMessages.Add(outbox);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new ConfirmScannedOrderResponse(
                order.Id.Value,
                order.Code,
                order.TotalAmount.Amount,
                "Confirmed",
                customer.Name,
                true
            ));
        });

        return app;
    }
}
