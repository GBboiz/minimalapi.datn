using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.GetCustomers;

public sealed class GetCustomersHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var query = db.Customers.Where(c => c.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower) || c.Phone.Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto(
                c.Id.Value,
                c.Name,
                c.Phone,
                c.Email,
                c.Address,
                c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>(items, totalCount, request.Page, request.PageSize);
    }
}
