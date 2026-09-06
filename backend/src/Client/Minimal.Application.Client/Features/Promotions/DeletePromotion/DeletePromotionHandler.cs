using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Promotions.DeletePromotion;

public sealed class DeletePromotionHandler(
    IPromotionRepository promotionRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePromotionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeletePromotionCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var promotion = await promotionRepo.GetByIdAsync(new PromotionId(request.Id), storeId, ct);
        if (promotion is null)
            return Result<Guid>.Failure("Khuyến mãi không tồn tại.");

        promotionRepo.Remove(promotion);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(promotion.Id.Value);
    }
}
