namespace MinimalAPI.Domain.Entities;

public readonly record struct StoreId(Guid Value)
{
    public static StoreId New() => new(Guid.NewGuid());
}
