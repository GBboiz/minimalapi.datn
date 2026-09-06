using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(PromotionId id, StoreId storeId, CancellationToken ct = default);
    Task<Promotion?> GetByCodeAsync(string code, StoreId storeId, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, StoreId storeId, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, PromotionId excludeId, StoreId storeId, CancellationToken ct = default);
    void Add(Promotion promotion);
    void Remove(Promotion promotion);
}
