using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IGoogleSheetConnectionRepository
{
    Task<GoogleSheetConnection?> GetByStoreIdAsync(StoreId storeId, CancellationToken ct = default);
    void Add(GoogleSheetConnection connection);
    void Update(GoogleSheetConnection connection);
}
