using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class GoogleSheetConnectionRepository(AppDbContext context) : IGoogleSheetConnectionRepository
{
    public async Task<GoogleSheetConnection?> GetByStoreIdAsync(StoreId storeId, CancellationToken ct = default)
    {
        return await context.GoogleSheetConnections
            .FirstOrDefaultAsync(c => c.StoreId == storeId, ct);
    }

    public void Add(GoogleSheetConnection connection)
    {
        context.GoogleSheetConnections.Add(connection);
    }

    public void Update(GoogleSheetConnection connection)
    {
        context.GoogleSheetConnections.Update(connection);
    }
}
