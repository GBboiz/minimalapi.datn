namespace MinimalAPI.Domain.Entities;

public readonly record struct OrderItemId(Guid Value)
{
    public static OrderItemId New() => new(Guid.NewGuid());
}
