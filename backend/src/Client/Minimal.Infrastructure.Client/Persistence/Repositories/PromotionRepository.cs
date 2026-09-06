using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class PromotionRepository(AppDbContext db) : IPromotionRepository
{
    public async Task<Promotion?> GetByIdAsync(PromotionId id, StoreId storeId, CancellationToken ct = default) =>
        await db.Promotions.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == storeId, ct);

    public async Task<Promotion?> GetByCodeAsync(string code, StoreId storeId, CancellationToken ct = default) =>
        await db.Promotions.FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpper() && p.StoreId == storeId, ct);

    public async Task<bool> ExistsByCodeAsync(string code, StoreId storeId, CancellationToken ct = default) =>
        await db.Promotions.AnyAsync(p => p.Code == code.Trim().ToUpper() && p.StoreId == storeId, ct);

    public async Task<bool> ExistsByCodeAsync(string code, PromotionId excludeId, StoreId storeId, CancellationToken ct = default) =>
        await db.Promotions.AnyAsync(p => p.Code == code.Trim().ToUpper() && p.Id != excludeId && p.StoreId == storeId, ct);

    public void Add(Promotion promotion) => db.Promotions.Add(promotion);

    public void Remove(Promotion promotion) => db.Promotions.Remove(promotion);
}
