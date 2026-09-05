using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IOutboxRepository
{
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10, CancellationToken ct = default);
    Task<List<OutboxMessage>> GetRecentByStoreIdAsync(StoreId storeId, int count = 20, CancellationToken ct = default);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(OutboxMessage message);
    void Update(OutboxMessage message);
}
