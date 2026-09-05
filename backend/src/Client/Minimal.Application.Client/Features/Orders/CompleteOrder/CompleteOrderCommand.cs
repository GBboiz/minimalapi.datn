using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Orders.CompleteOrder;

public record CompleteOrderCommand(Guid Id) : IRequest<Result<Guid>>;
