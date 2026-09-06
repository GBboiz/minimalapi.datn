namespace MinimalAPI.Application.Features.Promotions.DTOs;

public record PromotionDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    int DiscountType,
    decimal Value,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);
