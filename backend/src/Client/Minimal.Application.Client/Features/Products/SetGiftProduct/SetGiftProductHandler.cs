using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.SetGiftProduct;

public sealed class SetGiftProductHandler(
    IProductRepository productRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetGiftProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SetGiftProductCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var product = await productRepo.GetByIdAsync(new ProductId(request.ProductId), storeId, ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");

        if (request.GiftProductId.HasValue && request.GiftProductId.Value != Guid.Empty)
        {
            if (request.GiftProductId.Value == request.ProductId)
                return Result<Guid>.Failure("Không thể gán sản phẩm làm quà tặng cho chính nó.");

            var gift = await productRepo.GetByIdAsync(new ProductId(request.GiftProductId.Value), storeId, ct);
            if (gift is null)
                return Result<Guid>.Failure("Sản phẩm quà tặng không tồn tại.");

            product.SetGiftProduct(gift.Id, gift.Name.Value);
        }
        else
        {
            product.SetGiftProduct(null, null);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(product.Id.Value);
    }
}
