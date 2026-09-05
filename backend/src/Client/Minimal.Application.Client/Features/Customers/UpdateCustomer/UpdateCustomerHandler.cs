using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.UpdateCustomer;

public sealed class UpdateCustomerHandler(
    ICustomerRepository customerRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var customer = await customerRepo.GetByIdAsync(new CustomerId(request.Id), storeId, ct);
        if (customer is null)
            return Result<Guid>.Failure("Khách hàng không tồn tại.");

        if (await customerRepo.ExistsByPhoneAsync(request.Phone, new CustomerId(request.Id), storeId, ct))
            return Result<Guid>.Failure("Số điện thoại khách hàng đã tồn tại trong cửa hàng.");

        customer.UpdateInfo(request.Name, request.Phone, request.Email, request.Address);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(customer.Id.Value);
    }
}
