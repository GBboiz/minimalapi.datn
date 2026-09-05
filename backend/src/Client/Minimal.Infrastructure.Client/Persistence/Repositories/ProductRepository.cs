using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của IProductRepository.</summary>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(ProductId id, StoreId storeId, CancellationToken ct = default) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == storeId, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsBySkuAsync(string sku, StoreId storeId, CancellationToken ct = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        return await db.Products.AnyAsync(p => p.Sku == normalizedSku && p.StoreId == storeId, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsBySkuAsync(string sku, ProductId excludeId, StoreId storeId, CancellationToken ct = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        return await db.Products.AnyAsync(p => p.Sku == normalizedSku && p.Id != excludeId && p.StoreId == storeId, ct);
    }

    /// <inheritdoc />
    public void Add(Product product) => db.Products.Add(product);

    /// <inheritdoc />
    public void Remove(Product product) => db.Products.Remove(product);
}
