namespace MinimalAPI.Application.Features.Inventory.DTOs;

public record InventoryItemDto(
    Guid Id,
    string Sku,
    string Name,
    Guid CategoryId,
    string CategoryName,
    int StockQuantity,    // Tồn kho thực tế
    int ReservedQuantity, // Số lượng giữ chỗ (chờ xác nhận)
    int ForecastStock,    // Tồn kho dự báo khả dụng
    decimal Price,
    string Currency,
    bool IsActive,
    Guid? GiftProductId = null,
    string? GiftProductName = null);

public record InventorySummaryDto(
    int TotalProducts,
    int TotalActualStock,
    int TotalReservedStock,
    int TotalForecastStock,
    int LowStockAlertCount,
    List<InventoryItemDto> Items);
