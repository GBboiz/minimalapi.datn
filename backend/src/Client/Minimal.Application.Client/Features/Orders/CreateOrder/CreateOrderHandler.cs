using System.Text.Json;
using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

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

            orderItems.Add(OrderItem.Create(
                orderId,
                product.Id,
                product.Name.Value,
                product.Price,
                itemReq.Quantity));
        }

        // Sinh mã đơn ngẫu nhiên theo ngày ORD-YYYYMMDD-XXXX
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        string code;
        do
        {
            var randomSuffix = Random.Shared.Next(1000, 9999);
            code = $"ORD-{datePrefix}-{randomSuffix}";
        } while (await orderRepo.ExistsByCodeAsync(code, storeId, ct));

        var order = Order.Create(storeId, customer.Id, code, orderItems);
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
            order.CreatedAt);

        var outboxMessage = OutboxMessage.Create(
            storeId,
            "OrderCreated",
            JsonSerializer.Serialize(syncPayload));

        outboxRepo.Add(outboxMessage);

        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
