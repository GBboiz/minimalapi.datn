using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public record CreateOrderItemRequest(Guid ProductId, int Quantity);

public record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemRequest> Items) : IRequest<Result<Guid>>;
