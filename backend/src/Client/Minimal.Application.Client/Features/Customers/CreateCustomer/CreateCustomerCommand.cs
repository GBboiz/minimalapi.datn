using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Customers.CreateCustomer;

public record CreateCustomerCommand(
    string Name,
    string Phone,
    string? Email,
    string? Address) : IRequest<Result<Guid>>;
