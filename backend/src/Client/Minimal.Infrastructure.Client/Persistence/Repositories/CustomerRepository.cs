using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(CustomerId id, StoreId storeId, CancellationToken ct = default) =>
        await db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == storeId, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, StoreId storeId, CancellationToken ct = default) =>
        await db.Customers.AnyAsync(c => c.Phone == phone.Trim() && c.StoreId == storeId, ct);

    public async Task<bool> ExistsByPhoneAsync(string phone, CustomerId excludeId, StoreId storeId, CancellationToken ct = default) =>
        await db.Customers.AnyAsync(c => c.Phone == phone.Trim() && c.Id != excludeId && c.StoreId == storeId, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);

    public void Remove(Customer customer) => db.Customers.Remove(customer);
}
