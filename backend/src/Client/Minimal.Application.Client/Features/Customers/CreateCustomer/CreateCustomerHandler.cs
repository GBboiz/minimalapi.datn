using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.CreateCustomer;

public sealed class CreateCustomerHandler(
    ICustomerRepository customerRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        if (await customerRepo.ExistsByPhoneAsync(request.Phone, storeId, ct))
            return Result<Guid>.Failure("Số điện thoại khách hàng đã tồn tại trong cửa hàng.");

        var customer = Customer.Create(storeId, request.Name, request.Phone, request.Email, request.Address);

        customerRepo.Add(customer);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(customer.Id.Value);
    }
}
