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

        // Nếu đơn hàng đã từng được xác nhận thì hoàn trả tồn kho
        if (order.Status == OrderStatus.Confirmed)
        {
            foreach (var item in order.Items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, storeId, ct);
                if (product is not null)
                {
                    product.AdjustStock(item.Quantity);
                }
            }
        }

        order.Cancel();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
