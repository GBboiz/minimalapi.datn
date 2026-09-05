using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(
    ICategoryRepository categoryRepo,
    IProductRepository productRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        if (await productRepo.ExistsBySkuAsync(request.Sku, storeId, ct))
            return Result<Guid>.Failure("Mã SKU đã tồn tại trong cửa hàng.");

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), storeId, ct);
        if (category is null)
            return Result<Guid>.Failure("Danh mục không tồn tại.");

        var productName = ProductName.Create(request.Name);
        var productPrice = Money.Create(request.Price, request.Currency);

        var product = Product.Create(storeId, request.Sku, request.StockQuantity, productName, productPrice, new CategoryId(request.CategoryId), request.Description);

        productRepo.Add(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
