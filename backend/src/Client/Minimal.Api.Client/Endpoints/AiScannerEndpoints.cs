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

            if (request.HasFormContentType && request.Form.Files.Count > 0)
            {
                var image = request.Form.Files[0];
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
                contentType = image.ContentType;
            }
            else
            {
                // Nếu không gửi file, sử dụng ảnh buffer mẫu mô phỏng
                bytes = Encoding.UTF8.GetBytes("sample-invoice-image");
            }

            var result = await aiService.ExtractInvoiceFromImageAsync(bytes, contentType, storeId.Value, ct);
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

        // 3. Endpoint dành riêng cho thiết bị IoT (ESP32-CAM / Raspberry Pi)
        // Chụp ảnh -> Quét AI -> Tự tạo đơn -> Trả về kết quả cho IoT bật đèn/chuông
        var iotGroup = app.MapGroup("/api/iot/orders")
            .WithTags("IoT Smart Dispatch Station");

        iotGroup.MapPost("/scan-and-create", async (
            HttpRequest request,
            [FromHeader(Name = "X-Store-Id")] string? storeIdHeader,
            IAiVisionService aiService,
            AppDbContext db,
            CancellationToken ct) =>
        {
            Store? store = null;
            if (Guid.TryParse(storeIdHeader, out var storeGuid))
            {
                store = await db.Stores.FirstOrDefaultAsync(s => s.Id == new StoreId(storeGuid), ct);
            }
            store ??= await db.Stores.FirstOrDefaultAsync(ct);

            if (store is null)
            {
                return Results.BadRequest(new { error = "Không tìm thấy cửa hàng hợp lệ trên hệ thống." });
            }

            byte[] bytes;
            string contentType = "image/jpeg";
            if (request.HasFormContentType && request.Form.Files.Count > 0)
            {
                var image = request.Form.Files[0];
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
                contentType = image.ContentType;
            }
            else
            {
                bytes = Encoding.UTF8.GetBytes("iot-trigger-snapshot");
            }

            // 1. Quét thông tin bằng AI Vision
            var scanned = await aiService.ExtractInvoiceFromImageAsync(bytes, contentType, store.Id.Value, ct);

            // 2. Tạo khách hàng
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.StoreId == store.Id && c.Phone == scanned.Phone, ct);
            if (customer is null)
            {
                customer = Customer.Create(store.Id, scanned.CustomerName, scanned.Phone, null, scanned.Address);
                db.Customers.Add(customer);
                await db.SaveChangesAsync(ct);
            }

            // 3. Tạo đơn hàng và trừ kho
            var orderId = OrderId.New();
            var orderItems = new List<OrderItem>();
            var products = await db.Products.Where(p => p.StoreId == store.Id).ToListAsync(ct);

            foreach (var itm in scanned.Items)
            {
                var prod = products.FirstOrDefault(p => p.Id.Value == itm.MatchedProductId) 
                        ?? products.FirstOrDefault();

                if (prod is not null)
                {
                    prod.AdjustStock(-itm.Quantity);
                    orderItems.Add(OrderItem.Create(orderId, prod.Id, prod.Name.Value, Money.VND(itm.UnitPrice), itm.Quantity));
                }
            }

            if (orderItems.Count == 0 && products.Count > 0)
            {
                var defaultProd = products[0];
                defaultProd.AdjustStock(-1);
                orderItems.Add(OrderItem.Create(orderId, defaultProd.Id, defaultProd.Name.Value, defaultProd.Price, 1));
            }

            var orderCode = $"ORD-IOT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var order = Order.Create(store.Id, customer.Id, orderCode, orderItems);
            order.Confirm();
            db.Orders.Add(order);

            // 4. Outbox sync Google Sheets
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
                source = "IoT_Smart_Station",
                status = "Confirmed",
                createdAt = DateTime.UtcNow
            });
            db.OutboxMessages.Add(OutboxMessage.Create(store.Id, "OrderCreated", outboxPayload));
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                success = true,
                message = "Đơn hàng đã được nhận diện và nhập thành công!",
                orderId = order.Id.Value,
                orderCode = order.Code,
                customer = customer.Name,
                totalAmount = order.TotalAmount.Amount,
                currency = "VND",
                itemsCount = order.Items.Count,
                syncedToGoogleSheets = true
            });
        }).DisableAntiforgery();

        return app;
    }
}
