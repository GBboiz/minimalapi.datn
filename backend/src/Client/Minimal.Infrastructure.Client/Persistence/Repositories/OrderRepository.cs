using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(OrderId id, StoreId storeId, CancellationToken ct = default) =>
        await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.StoreId == storeId, ct);

    public async Task<bool> ExistsByCodeAsync(string code, StoreId storeId, CancellationToken ct = default) =>
        await db.Orders.AnyAsync(o => o.Code == code.Trim().ToUpperInvariant() && o.StoreId == storeId, ct);

    public void Add(Order order) => db.Orders.Add(order);

    public void Remove(Order order) => db.Orders.Remove(order);
}
