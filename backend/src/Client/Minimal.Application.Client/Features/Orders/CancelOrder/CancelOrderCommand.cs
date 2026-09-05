using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Orders.CancelOrder;

public record CancelOrderCommand(Guid Id) : IRequest<Result<Guid>>;
