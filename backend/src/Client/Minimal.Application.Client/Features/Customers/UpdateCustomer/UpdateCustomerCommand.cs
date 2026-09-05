using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Customers.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? Address) : IRequest<Result<Guid>>;
