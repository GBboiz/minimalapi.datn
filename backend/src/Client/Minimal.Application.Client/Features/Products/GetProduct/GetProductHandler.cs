using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Products.GetProduct;

public sealed class GetProductHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetProductQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken ct)
    {
        var productId = new ProductId(request.Id);

        var storeId = currentStore.GetRequiredStoreId();
        return await db.Products
            .Where(p => p.Id == productId && p.StoreId == storeId)
            .Join(db.Categories,
                p => p.CategoryId, c => c.Id,
                (p, c) => new ProductDto(
                    p.Id.Value,
                    p.Name.Value,
                    p.Sku,
                    p.StockQuantity,
                    p.Price.Amount,
                    p.Price.Currency,
                    p.CategoryId.Value,
                    c.Name,
                    p.Description,
                    p.IsActive,
                    p.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
