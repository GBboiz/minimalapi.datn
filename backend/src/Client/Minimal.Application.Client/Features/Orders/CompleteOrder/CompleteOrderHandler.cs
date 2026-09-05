using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.CompleteOrder;

public sealed class CompleteOrderHandler(
    IOrderRepository orderRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CompleteOrderCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var order = await orderRepo.GetByIdAsync(new OrderId(request.Id), storeId, ct);
        if (order is null)
            return Result<Guid>.Failure("Đơn hàng không tồn tại.");

        if (order.Status != OrderStatus.Confirmed)
            return Result<Guid>.Failure($"Chỉ có thể hoàn tất đơn hàng đã được xác nhận (trạng thái hiện tại: {order.Status}).");

        order.Complete();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id.Value);
    }
}
