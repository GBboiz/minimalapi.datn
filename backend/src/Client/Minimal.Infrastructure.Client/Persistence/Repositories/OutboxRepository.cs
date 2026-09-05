using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository(AppDbContext context) : IOutboxRepository
{
    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10, CancellationToken ct = default)
    {
        return await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<List<OutboxMessage>> GetRecentByStoreIdAsync(StoreId storeId, int count = 20, CancellationToken ct = default)
    {
        return await context.OutboxMessages
            .Where(m => m.StoreId == storeId)
            .OrderByDescending(m => m.OccurredAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public void Add(OutboxMessage message)
    {
        context.OutboxMessages.Add(message);
    }

    public void Update(OutboxMessage message)
    {
        context.OutboxMessages.Update(message);
    }
}
