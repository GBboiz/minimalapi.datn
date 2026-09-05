using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class StoreMember : Entity<Guid>
{
    public StoreId StoreId { get; private set; }
    public UserId UserId { get; private set; }
    public StoreRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StoreMember() { }

    public static StoreMember Create(StoreId storeId, UserId userId, StoreRole role) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        UserId = userId,
        Role = role,
        CreatedAt = DateTime.UtcNow
    };
}
