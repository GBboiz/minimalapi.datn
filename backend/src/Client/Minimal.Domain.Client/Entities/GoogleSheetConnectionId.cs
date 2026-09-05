namespace MinimalAPI.Domain.Entities;

public readonly record struct GoogleSheetConnectionId(Guid Value)
{
    public static GoogleSheetConnectionId New() => new(Guid.NewGuid());
    public static GoogleSheetConnectionId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
