using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.ConfirmOrder;

public sealed class ConfirmOrderHandler(
    IOrderRepository orderRepo,
    IProductRepository productRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var order = await orderRepo.GetByIdAsync(new OrderId(request.Id), storeId, ct);
        if (order is null)
            return Result<Guid>.Failure("Đơn hàng không tồn tại.");

        if (order.Status != OrderStatus.Pending)
            return Result<Guid>.Failure($"Không thể xác nhận đơn hàng đang ở trạng thái {order.Status}.");

        // 1. Kiểm tra tồn kho cho tất cả sản phẩm trước khi trừ
        var productsToUpdate = new List<(Product Product, int Quantity)>();

        foreach (var item in order.Items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId, storeId, ct);
            if (product is null)
                return Result<Guid>.Failure($"Sản phẩm '{item.ProductName}' không còn tồn tại.");

            if (product.StockQuantity < item.Quantity)
            {
                return Result<Guid>.Failure(
                    $"Sản phẩm '{product.Name.Value}' không đủ tồn kho (còn {product.StockQuantity}, cần {item.Quantity}).");
            }

            productsToUpdate.Add((product, item.Quantity));
        }

        // 2. Trừ tồn kho
        foreach (var (product, quantity) in productsToUpdate)
        {
            product.AdjustStock(-quantity);
        }

        // 3. Đổi trạng thái sang Confirmed
        order.Confirm();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
