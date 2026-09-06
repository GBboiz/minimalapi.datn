using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Promotions.UpdatePromotion;

public sealed class UpdatePromotionHandler(
    IPromotionRepository promotionRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePromotionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdatePromotionCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var promotion = await promotionRepo.GetByIdAsync(new PromotionId(request.Id), storeId, ct);
        if (promotion is null)
            return Result<Guid>.Failure("Khuyến mãi không tồn tại.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Guid>.Failure("Tên chương trình không được để trống.");

        if (!Enum.IsDefined(typeof(DiscountType), request.DiscountType))
            return Result<Guid>.Failure("Loại khuyến mãi không hợp lệ.");

        var type = (DiscountType)request.DiscountType;
        if (request.Value <= 0)
            return Result<Guid>.Failure("Giá trị khuyến mãi phải lớn hơn 0.");

        if (type == DiscountType.Percentage && request.Value > 100)
            return Result<Guid>.Failure("Phần trăm giảm giá không được vượt quá 100%.");

        promotion.UpdateInfo(request.Name, type, request.Value, request.Description, request.IsActive);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(promotion.Id.Value);
    }
}
