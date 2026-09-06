using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Promotions.DTOs;

namespace MinimalAPI.Application.Features.Promotions.GetPromotions;

public sealed class GetPromotionsHandler(IApplicationDbContext db, ICurrentStore currentStore)
    : IRequestHandler<GetPromotionsQuery, List<PromotionDto>>
{
    public async Task<List<PromotionDto>> Handle(GetPromotionsQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var query = db.Promotions.Where(p => p.StoreId == storeId);

        if (request.OnlyActive == true)
        {
            query = query.Where(p => p.IsActive);
        }

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return list.Select(p => new PromotionDto(
            p.Id.Value,
            p.Code,
            p.Name,
            p.Type.ToString(),
            (int)p.Type,
            p.Value,
            p.Description,
            p.IsActive,
            p.CreatedAt)).ToList();
    }
}
