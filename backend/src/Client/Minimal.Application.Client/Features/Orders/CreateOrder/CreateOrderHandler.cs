using System.Text.Json;
using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IOrderRepository orderRepo,
    IOutboxRepository outboxRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var customer = await customerRepo.GetByIdAsync(new CustomerId(request.CustomerId), storeId, ct);
        if (customer is null)
            return Result<Guid>.Failure("Khách hàng không tồn tại trong cửa hàng.");

        var orderId = OrderId.New();
        var orderItems = new List<OrderItem>();

        foreach (var itemReq in request.Items)
        {
            var product = await productRepo.GetByIdAsync(new ProductId(itemReq.ProductId), storeId, ct);
            if (product is null)
                return Result<Guid>.Failure($"Sản phẩm với Id {itemReq.ProductId} không tồn tại.");

            if (!product.IsActive)
                return Result<Guid>.Failure($"Sản phẩm '{product.Name.Value}' đã ngưng hoạt động.");

            var itemPrice = itemReq.UnitPrice.HasValue && itemReq.UnitPrice.Value >= 0 && !itemReq.IsGift
                ? Money.Create(itemReq.UnitPrice.Value, product.Price.Currency)
                : product.Price;

            orderItems.Add(OrderItem.Create(
                orderId,
                product.Id,
                product.Name.Value,
                itemPrice,
                itemReq.Quantity,
                isGift: itemReq.IsGift));

            // Tự động tặng quà kèm theo cấu hình sản phẩm nếu chưa có trong đơn
            if (!itemReq.IsGift && product.GiftProductId is not null)
            {
                var giftId = product.GiftProductId.Value;
                var alreadyInRequest = request.Items.Any(i => i.ProductId == giftId.Value && i.IsGift);
                var alreadyInOrder = orderItems.Any(i => i.ProductId == giftId && i.IsGift);

                if (!alreadyInRequest && !alreadyInOrder)
                {
                    var giftProduct = await productRepo.GetByIdAsync(giftId, storeId, ct);
                    if (giftProduct is not null && giftProduct.IsActive)
                    {
                        orderItems.Add(OrderItem.Create(
                            orderId,
                            giftProduct.Id,
                            giftProduct.Name.Value,
                            giftProduct.Price,
                            itemReq.Quantity,
                            isGift: true));
                    }
                }
            }
        }

        // Kiểm tra và giữ chỗ tồn kho dự báo (ReserveStock) cho từng sản phẩm (kể cả quà tặng)
        var productQuantities = orderItems
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
            .ToList();

        foreach (var pq in productQuantities)
        {
            var product = await productRepo.GetByIdAsync(pq.ProductId, storeId, ct);
            if (product is null)
                return Result<Guid>.Failure("Sản phẩm không tồn tại.");

            if (product.ForecastStock < pq.TotalQuantity)
            {
                return Result<Guid>.Failure(
                    $"Sản phẩm '{product.Name.Value}' không đủ tồn kho dự báo (khả dụng: {product.ForecastStock}, cần: {pq.TotalQuantity}).");
            }

            product.ReserveStock(pq.TotalQuantity);
        }

        // Sinh mã đơn ngẫu nhiên theo ngày ORD-YYYYMMDD-XXXX
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        string code;
        do
        {
            var randomSuffix = Random.Shared.Next(1000, 9999);
            code = $"ORD-{datePrefix}-{randomSuffix}";
        } while (await orderRepo.ExistsByCodeAsync(code, storeId, ct));

        var order = Order.Create(storeId, customer.Id, code, orderItems, request.DiscountPercent, request.DiscountAmount);
        orderRepo.Add(order);

        // Transactional Outbox Pattern: Lưu OutboxMessage cùng transaction với Order
        var productsSummary = string.Join(", ", orderItems.Select(i => $"{i.ProductName} (x{i.Quantity})"));
        var totalQuantity = orderItems.Sum(i => i.Quantity);
        var syncPayload = new OrderSyncPayload(
            order.Id.Value,
            order.Code,
            customer.Name,
            customer.Phone,
            productsSummary,
            totalQuantity,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Status.ToString(),
            order.CreatedAt,
            order.SubTotal.Amount,
            order.DiscountPercent,
            order.DiscountAmount.Amount);

        var outboxMessage = OutboxMessage.Create(
            storeId,
            "OrderCreated",
            JsonSerializer.Serialize(syncPayload));

        outboxRepo.Add(outboxMessage);

        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
