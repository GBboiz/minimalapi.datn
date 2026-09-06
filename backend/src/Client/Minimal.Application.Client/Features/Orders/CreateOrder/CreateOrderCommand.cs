using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public record CreateOrderItemRequest(Guid ProductId, int Quantity, bool IsGift = false);

public record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemRequest> Items,
    decimal DiscountPercent = 0,
    decimal DiscountAmount = 0,
    string? PromotionCode = null) : IRequest<Result<Guid>>;
