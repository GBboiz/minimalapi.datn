using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Customers.GetCustomer;

public sealed class GetCustomerHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetCustomerQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var customerId = new CustomerId(request.Id);

        return await db.Customers
            .Where(c => c.Id == customerId && c.StoreId == storeId)
            .Select(c => new CustomerDto(
                c.Id.Value,
                c.Name,
                c.Phone,
                c.Email,
                c.Address,
                c.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
