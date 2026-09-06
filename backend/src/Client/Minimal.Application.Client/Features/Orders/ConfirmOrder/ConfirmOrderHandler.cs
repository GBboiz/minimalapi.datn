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

        // 1. Nhóm theo ProductId và kiểm tra tồn kho cho tất cả sản phẩm (kể cả quà tặng) trước khi trừ
        var groupedItems = order.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().ProductName,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .ToList();

        var productsToUpdate = new List<(Product Product, int Quantity)>();

        foreach (var item in groupedItems)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId, storeId, ct);
            if (product is null)
                return Result<Guid>.Failure($"Sản phẩm '{item.ProductName}' không còn tồn tại.");

            if (product.StockQuantity < item.TotalQuantity)
            {
                return Result<Guid>.Failure(
                    $"Sản phẩm '{product.Name.Value}' không đủ tồn kho (còn {product.StockQuantity}, cần {item.TotalQuantity}).");
            }

            productsToUpdate.Add((product, item.TotalQuantity));
        }

        // 2. Trừ tồn kho thực tế và giải phóng tồn dự báo đã giữ chỗ
        foreach (var (product, quantity) in productsToUpdate)
        {
            product.ConfirmReservedStock(quantity);
        }

        // 3. Đổi trạng thái sang Confirmed
        order.Confirm();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
