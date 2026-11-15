using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository(AppDbContext context) : IOutboxRepository
{
    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10, CancellationToken ct = default)
    {
        // Chỉ lấy tin nhắn của cửa hàng đã cấp quyền Google. Tin nhắn của cửa hàng chưa kết nối
        // được giữ nguyên và tự đồng bộ bù khi cửa hàng kết nối — không chặn hàng chờ của cửa hàng khác.
        var syncableStatuses = new[] { GoogleSheetConnectionStatus.Connected, GoogleSheetConnectionStatus.SyncError };
        return await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .Where(m => context.GoogleSheetConnections.Any(c =>
                c.StoreId == m.StoreId
                && syncableStatuses.Contains(c.Status)
                && c.EncryptedRefreshToken != ""))
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
