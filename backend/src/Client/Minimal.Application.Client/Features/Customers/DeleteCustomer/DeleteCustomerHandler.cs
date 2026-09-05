using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.DeleteCustomer;

public sealed class DeleteCustomerHandler(
    ICustomerRepository customerRepo,
    IApplicationDbContext db,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var customerId = new CustomerId(request.Id);

        var customer = await customerRepo.GetByIdAsync(customerId, storeId, ct);
        if (customer is null)
            return Result<Guid>.Failure("Khách hàng không tồn tại.");

        var hasOrders = await db.Orders.AnyAsync(o => o.CustomerId == customerId && o.StoreId == storeId, ct);
        if (hasOrders)
            return Result<Guid>.Failure("Không thể xóa khách hàng đã có đơn hàng trong hệ thống.");

        customerRepo.Remove(customer);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(customer.Id.Value);
    }
}
