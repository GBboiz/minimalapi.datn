using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Categories.GetCategory;

public sealed class GetCategoryHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetCategoryQuery, CategoryDto?>
{
    public async Task<CategoryDto?> Handle(GetCategoryQuery request, CancellationToken ct)
    {
        var categoryId = new CategoryId(request.Id);

        var storeId = currentStore.GetRequiredStoreId();
        return await db.Categories
            .Where(c => c.Id == categoryId && c.StoreId == storeId)
            .Select(c => new CategoryDto(
                c.Id.Value,
                c.Name,
                c.Description,
                c.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
