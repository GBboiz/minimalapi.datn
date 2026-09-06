using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Promotions.CreatePromotion;

public sealed class CreatePromotionHandler(
    IPromotionRepository promotionRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePromotionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePromotionCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();

        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<Guid>.Failure("Mã khuyến mãi không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Guid>.Failure("Tên chương trình không được để trống.");

        if (await promotionRepo.ExistsByCodeAsync(request.Code, storeId, ct))
            return Result<Guid>.Failure($"Mã khuyến mãi '{request.Code}' đã tồn tại.");

        if (!Enum.IsDefined(typeof(DiscountType), request.DiscountType))
            return Result<Guid>.Failure("Loại khuyến mãi không hợp lệ.");

        var type = (DiscountType)request.DiscountType;
        if (request.Value <= 0)
            return Result<Guid>.Failure("Giá trị khuyến mãi phải lớn hơn 0.");

        if (type == DiscountType.Percentage && request.Value > 100)
            return Result<Guid>.Failure("Phần trăm giảm giá không được vượt quá 100%.");

        var promotion = Promotion.Create(
            storeId,
            request.Code,
            request.Name,
            type,
            request.Value,
            request.Description);

        promotionRepo.Add(promotion);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(promotion.Id.Value);
    }
}
