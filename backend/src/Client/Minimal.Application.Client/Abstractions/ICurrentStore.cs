using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Abstractions;

/// <summary>Store được chọn từ claim của access token hiện tại.</summary>
public interface ICurrentStore
{
    StoreId GetRequiredStoreId();
}
