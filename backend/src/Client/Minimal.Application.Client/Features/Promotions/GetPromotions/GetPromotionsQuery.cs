using MediatR;
using MinimalAPI.Application.Features.Promotions.DTOs;

namespace MinimalAPI.Application.Features.Promotions.GetPromotions;

public record GetPromotionsQuery(bool? OnlyActive = null) : IRequest<List<PromotionDto>>;
