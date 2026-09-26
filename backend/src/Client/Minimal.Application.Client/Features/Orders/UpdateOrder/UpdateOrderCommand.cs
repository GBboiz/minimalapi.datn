using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Orders.UpdateOrder;

public record UpdateOrderItemRequest(Guid ProductId, int Quantity, bool IsGift = false, decimal? UnitPrice = null);

public record UpdateOrderCommand(
    Guid Id,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? CustomerAddress = null,
    List<UpdateOrderItemRequest>? Items = null,
    decimal? DiscountPercent = null,
    decimal? DiscountAmount = null
) : IRequest<Result<Guid>>;
