using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Product.</summary>
public interface IProductRepository
{
    /// <summary>Lấy sản phẩm theo Id (null nếu không tìm thấy).</summary>
    Task<Product?> GetByIdAsync(ProductId id, StoreId storeId, CancellationToken ct = default);

    /// <summary>Kiểm tra mã SKU đã tồn tại trong cửa hàng chưa.</summary>
    Task<bool> ExistsBySkuAsync(string sku, StoreId storeId, CancellationToken ct = default);

    /// <summary>Kiểm tra mã SKU đã tồn tại trong cửa hàng chưa (loại trừ sản phẩm hiện tại).</summary>
    Task<bool> ExistsBySkuAsync(string sku, ProductId excludeId, StoreId storeId, CancellationToken ct = default);

    /// <summary>Thêm sản phẩm mới vào DbContext.</summary>
    void Add(Product product);

    /// <summary>Đánh dấu sản phẩm để xóa.</summary>
    void Remove(Product product);
}
