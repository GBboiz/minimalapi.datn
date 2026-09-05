using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class OutboxMessage : Entity<Guid>
{
    public StoreId StoreId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(StoreId storeId, string type, string payload) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        Type = type,
        Payload = payload,
        OccurredAt = DateTime.UtcNow,
        RetryCount = 0
    };

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    public void RecordFailure(string error)
    {
        RetryCount++;
        Error = error;
    }

    public void ResetForRetry()
    {
        Error = null;
        ProcessedAt = null;
        RetryCount = 0;
    }
}
