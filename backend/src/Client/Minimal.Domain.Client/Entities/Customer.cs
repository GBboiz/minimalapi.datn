using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public StoreId StoreId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Customer() { }

    public static Customer Create(
        StoreId storeId,
        string name,
        string phone,
        string? email,
        string? address) => new()
    {
        Id = CustomerId.New(),
        StoreId = storeId,
        Name = name.Trim(),
        Phone = phone.Trim(),
        Email = email?.Trim().ToLowerInvariant(),
        Address = address?.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    public void UpdateInfo(string name, string phone, string? email, string? address)
    {
        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Address = address?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
