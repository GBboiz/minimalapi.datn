using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Orders.GetOrders;

public sealed class GetOrdersHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();

        var query = db.Orders
            .Where(o => o.StoreId == storeId)
            .Join(db.Customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { Order = o, Customer = c });

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<OrderStatus>(request.Status, true, out var filterStatus))
        {
            query = query.Where(x => x.Order.Status == filterStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(x =>
                x.Order.Code.ToLower().Contains(searchLower) ||
                x.Customer.Name.ToLower().Contains(searchLower) ||
                x.Customer.Phone.Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.Order.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OrderSummaryDto(
                x.Order.Id.Value,
                x.Customer.Id.Value,
                x.Customer.Name,
                x.Customer.Phone,
                x.Order.Code,
                x.Order.Status.ToString(),
                x.Order.SubTotal.Amount,
                x.Order.DiscountPercent,
                x.Order.DiscountAmount.Amount,
                x.Order.TotalAmount.Amount,
                x.Order.TotalAmount.Currency,
                x.Order.Items.Sum(i => i.Quantity),
                x.Order.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<OrderSummaryDto>(items, totalCount, request.Page, request.PageSize);
    }
}
