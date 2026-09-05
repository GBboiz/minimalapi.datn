using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Events;

public sealed record OrderCreatedEvent(OrderId OrderId, StoreId StoreId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
