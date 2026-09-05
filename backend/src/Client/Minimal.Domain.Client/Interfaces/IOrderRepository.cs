using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, StoreId storeId, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, StoreId storeId, CancellationToken ct = default);
    void Add(Order order);
    void Remove(Order order);
}
