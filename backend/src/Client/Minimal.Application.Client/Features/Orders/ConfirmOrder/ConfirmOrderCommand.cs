using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Orders.ConfirmOrder;

public record ConfirmOrderCommand(Guid Id) : IRequest<Result<Guid>>;
