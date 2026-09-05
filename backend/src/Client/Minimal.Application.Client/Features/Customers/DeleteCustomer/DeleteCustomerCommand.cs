using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Customers.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id) : IRequest<Result<Guid>>;
