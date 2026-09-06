using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventory.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Inventory.GetInventory;

public sealed class GetInventoryHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetInventoryQuery, InventorySummaryDto>
{
    public async Task<InventorySummaryDto> Handle(GetInventoryQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();

        var query = db.Products
            .Where(p => p.StoreId == storeId)
            .Join(db.Categories,
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Product = p, CategoryName = c.Name });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Product.Name.Value.ToLower().Contains(search) ||
                                     x.Product.Sku.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
        {
            var catId = new CategoryId(request.CategoryId.Value);
            query = query.Where(x => x.Product.CategoryId == catId);
        }

        var allItems = await query
            .OrderBy(x => x.Product.StockQuantity - x.Product.ReservedQuantity)
            .ToListAsync(ct);

        var dtos = allItems.Select(x => new InventoryItemDto(
            x.Product.Id.Value,
            x.Product.Sku,
            x.Product.Name.Value,
            x.Product.CategoryId.Value,
            x.CategoryName,
            x.Product.StockQuantity,
            x.Product.ReservedQuantity,
            x.Product.ForecastStock,
            x.Product.Price.Amount,
            x.Product.Price.Currency,
            x.Product.IsActive,
            x.Product.GiftProductId?.Value,
            x.Product.GiftProductName)).ToList();

        if (request.LowStockOnly == true)
        {
            dtos = dtos.Where(d => d.ForecastStock <= 5).ToList();
        }

        var totalProducts = dtos.Count;
        var totalActualStock = dtos.Sum(d => d.StockQuantity);
        var totalReservedStock = dtos.Sum(d => d.ReservedQuantity);
        var totalForecastStock = dtos.Sum(d => d.ForecastStock);
        var lowStockAlertCount = dtos.Count(d => d.ForecastStock <= 5);

        return new InventorySummaryDto(
            totalProducts,
            totalActualStock,
            totalReservedStock,
            totalForecastStock,
            lowStockAlertCount,
            dtos);
    }
}
