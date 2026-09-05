using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class User : AggregateRoot<UserId>
{
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string email, string fullName, string passwordHash) => new()
    {
        Id = UserId.New(),
        Email = email.Trim().ToLowerInvariant(),
        FullName = fullName.Trim(),
        PasswordHash = passwordHash,
        CreatedAt = DateTime.UtcNow
    };

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void UpdateFullName(string fullName)
    {
        FullName = fullName.Trim();
    }
}
