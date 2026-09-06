namespace MinimalAPI.Application.Features.Products.DTOs;

/// <summary>DTO trả về thông tin sản phẩm cho client.</summary>
public record ProductDto(
    /// <summary>Mã sản phẩm.</summary>
    Guid Id,
    /// <summary>Tên sản phẩm.</summary>
    string Name,
    string Sku,
    int StockQuantity,
    /// <summary>Giá sản phẩm.</summary>
    decimal Price,
    /// <summary>Mã tiền tệ (VND, USD...).</summary>
    string Currency,
    /// <summary>Mã danh mục.</summary>
    Guid CategoryId,
    /// <summary>Tên danh mục.</summary>
    string CategoryName,
    /// <summary>Mô tả sản phẩm.</summary>
    string? Description,
    /// <summary>Trạng thái hoạt động.</summary>
    bool IsActive,
    /// <summary>Ngày tạo.</summary>
    DateTime CreatedAt,
    /// <summary>Mã sản phẩm quà tặng kèm.</summary>
    Guid? GiftProductId = null,
    /// <summary>Tên sản phẩm quà tặng kèm.</summary>
    string? GiftProductName = null,
    /// <summary>Số lượng giữ chỗ (đơn chờ duyệt).</summary>
    int ReservedQuantity = 0,
    /// <summary>Tồn kho dự báo khả dụng.</summary>
    int ForecastStock = 0);
