namespace MinimalAPI.Domain.Entities;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
}
