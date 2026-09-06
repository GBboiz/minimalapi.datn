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
        var item = await db.Products
            .Where(p => p.Id == productId && p.StoreId == storeId)
            .Join(db.Categories,
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Product = p, CategoryName = c.Name })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return null;

        return new ProductDto(
            item.Product.Id.Value,
            item.Product.Name.Value,
            item.Product.Sku,
            item.Product.StockQuantity,
            item.Product.Price.Amount,
            item.Product.Price.Currency,
            item.Product.CategoryId.Value,
            item.CategoryName,
            item.Product.Description,
            item.Product.IsActive,
            item.Product.CreatedAt,
            item.Product.GiftProductId?.Value,
            item.Product.GiftProductName,
            item.Product.ReservedQuantity,
            item.Product.ForecastStock);
    }
}
