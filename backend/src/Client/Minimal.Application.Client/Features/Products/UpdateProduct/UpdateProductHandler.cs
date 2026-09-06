using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), storeId, ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");

        if (await productRepo.ExistsBySkuAsync(request.Sku, new ProductId(request.Id), storeId, ct))
            return Result<Guid>.Failure("Mã SKU đã tồn tại trong cửa hàng.");

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), storeId, ct);
        if (category is null)
            return Result<Guid>.Failure("Danh mục không tồn tại.");

        var productName = ProductName.Create(request.Name);
        var productPrice = Money.Create(request.Price, request.Currency);

        ProductId? giftProductId = null;
        string? giftProductName = null;
        if (request.GiftProductId.HasValue && request.GiftProductId.Value != Guid.Empty)
        {
            var giftProduct = await productRepo.GetByIdAsync(new ProductId(request.GiftProductId.Value), storeId, ct);
            if (giftProduct is not null)
            {
                giftProductId = giftProduct.Id;
                giftProductName = giftProduct.Name.Value;
            }
        }

        product.UpdateInfo(request.Sku, productName, new CategoryId(request.CategoryId), request.Description, giftProductId, giftProductName);
        product.SetStockQuantity(request.StockQuantity);
        product.UpdatePrice(productPrice);

        if (request.IsActive)
            product.Activate();
        else
            product.Deactivate();

        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
