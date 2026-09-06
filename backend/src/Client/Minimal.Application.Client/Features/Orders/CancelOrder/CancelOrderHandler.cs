using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler(
    IOrderRepository orderRepo,
    IProductRepository productRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var order = await orderRepo.GetByIdAsync(new OrderId(request.Id), storeId, ct);
        if (order is null)
            return Result<Guid>.Failure("Đơn hàng không tồn tại.");

        if (order.Status == OrderStatus.Completed)
            return Result<Guid>.Failure("Không thể hủy đơn hàng đã hoàn tất.");

        if (order.Status == OrderStatus.Cancelled)
            return Result<Guid>.Success(order.Id.Value);

        var groupedItems = order.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
            .ToList();

        // Nếu đơn đang Pending: giải phóng số lượng giữ chỗ (tồn kho dự báo tăng lại)
        if (order.Status == OrderStatus.Pending)
        {
            foreach (var item in groupedItems)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, storeId, ct);
                if (product is not null)
                {
                    product.ReleaseReservedStock(item.TotalQuantity);
                }
            }
        }
        // Nếu đơn hàng đã từng được xác nhận: hoàn trả tồn kho thực tế
        else if (order.Status == OrderStatus.Confirmed)
        {
            foreach (var item in groupedItems)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, storeId, ct);
                if (product is not null)
                {
                    product.AdjustStock(item.TotalQuantity);
                }
            }
        }

        order.Cancel();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
