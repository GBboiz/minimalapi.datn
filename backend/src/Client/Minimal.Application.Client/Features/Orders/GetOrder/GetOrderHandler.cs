using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Orders.GetOrder;

public sealed class GetOrderHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var orderId = new OrderId(request.Id);

        var orderWithCustomer = await db.Orders
            .Where(o => o.Id == orderId && o.StoreId == storeId)
            .Join(db.Customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { Order = o, Customer = c })
            .FirstOrDefaultAsync(ct);

        if (orderWithCustomer is null)
            return null;

        var items = orderWithCustomer.Order.Items
            .Select(i => new OrderItemDto(
                i.Id.Value,
                i.ProductId.Value,
                i.ProductName,
                i.UnitPrice.Amount,
                i.UnitPrice.Currency,
                i.Quantity,
                i.TotalPrice,
                i.IsGift))
            .ToList();

        return new OrderDto(
            orderWithCustomer.Order.Id.Value,
            orderWithCustomer.Customer.Id.Value,
            orderWithCustomer.Customer.Name,
            orderWithCustomer.Customer.Phone,
            orderWithCustomer.Customer.Address,
            orderWithCustomer.Order.Code,
            orderWithCustomer.Order.Status.ToString(),
            orderWithCustomer.Order.SubTotal.Amount,
            orderWithCustomer.Order.DiscountPercent,
            orderWithCustomer.Order.DiscountAmount.Amount,
            orderWithCustomer.Order.TotalAmount.Amount,
            orderWithCustomer.Order.TotalAmount.Currency,
            orderWithCustomer.Order.CreatedAt,
            items);
    }
}
