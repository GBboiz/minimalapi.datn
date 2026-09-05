using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class Store : AggregateRoot<StoreId>
{
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private Store() { }

    public static Store Create(UserId ownerId, string name, string slug) => new()
    {
        Id = StoreId.New(),
        OwnerId = ownerId,
        Name = name.Trim(),
        Slug = slug.Trim().ToLowerInvariant(),
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string name, string slug)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
    }
}
