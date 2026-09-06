using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventory.AdjustInventory;

public sealed class AdjustInventoryHandler(
    IProductRepository productRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustInventoryCommand, Result<int>>
{
    public async Task<Result<int>> Handle(AdjustInventoryCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var product = await productRepo.GetByIdAsync(new ProductId(request.ProductId), storeId, ct);
        if (product is null)
            return Result<int>.Failure("Sản phẩm không tồn tại.");

        if (product.StockQuantity + request.StockChange < 0)
            return Result<int>.Failure($"Không thể điều chỉnh vì tồn kho thực tế sẽ bị âm (hiện tại: {product.StockQuantity}, điều chỉnh: {request.StockChange}).");

        product.AdjustStock(request.StockChange);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<int>.Success(product.StockQuantity);
    }
}
