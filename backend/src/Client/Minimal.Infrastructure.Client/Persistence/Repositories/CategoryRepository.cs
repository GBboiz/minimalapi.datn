using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của ICategoryRepository.</summary>
public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(CategoryId id, StoreId storeId, CancellationToken ct = default) =>
        await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == storeId, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, StoreId storeId, CancellationToken ct = default) =>
        await db.Categories.AnyAsync(c => c.Name == name && c.StoreId == storeId, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CategoryId excludeId, StoreId storeId, CancellationToken ct = default) =>
        await db.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId && c.StoreId == storeId, ct);

    /// <inheritdoc />
    public void Add(Category category) => db.Categories.Add(category);

    /// <inheritdoc />
    public void Remove(Category category) => db.Categories.Remove(category);
}
