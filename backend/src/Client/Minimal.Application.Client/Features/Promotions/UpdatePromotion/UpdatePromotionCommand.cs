using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Promotions.UpdatePromotion;

public record UpdatePromotionCommand(
    Guid Id,
    string Name,
    int DiscountType,
    decimal Value,
    string? Description,
    bool IsActive) : IRequest<Result<Guid>>;
