using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, StoreId storeId, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, StoreId storeId, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, CustomerId excludeId, StoreId storeId, CancellationToken ct = default);
    void Add(Customer customer);
    void Remove(Customer customer);
}
