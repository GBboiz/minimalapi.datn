using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Promotions.CreatePromotion;

public record CreatePromotionCommand(
    string Code,
    string Name,
    int DiscountType, // 1: Percentage, 2: FixedAmount
    decimal Value,
    string? Description = null) : IRequest<Result<Guid>>;
