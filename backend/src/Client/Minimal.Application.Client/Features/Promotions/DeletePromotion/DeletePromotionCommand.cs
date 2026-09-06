using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Promotions.DeletePromotion;

public record DeletePromotionCommand(Guid Id) : IRequest<Result<Guid>>;
