namespace MinimalAPI.Domain.Entities;

public readonly record struct PromotionId(Guid Value)
{
    public static PromotionId New() => new(Guid.NewGuid());
}
