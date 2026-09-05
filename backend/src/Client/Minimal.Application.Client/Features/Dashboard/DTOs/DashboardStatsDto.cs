namespace MinimalAPI.Application.Features.Dashboard.DTOs;

public sealed record LowStockProductDto(
    Guid Id,
    string Name,
    string Sku,
    int StockQuantity,
    decimal Price,
    string Currency
);

public sealed record RecentOrderDto(
    Guid Id,
    string Code,
    string CustomerName,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTime CreatedAt
);

public sealed record DashboardStatsDto(
    decimal TodayRevenue,
    int TodayOrders,
    int TodayCustomers,
    decimal TotalRevenue,
    string Currency,
    int TotalOrders,
    int PendingOrders,
    int ConfirmedOrders,
    int CompletedOrders,
    int CancelledOrders,
    int TotalCustomers,
    int TotalProducts,
    int LowStockProductsCount,
    List<LowStockProductDto> LowStockProducts,
    List<RecentOrderDto> RecentOrders
);
