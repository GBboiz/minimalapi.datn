using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Dashboard.DTOs;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Dashboard.GetStats;

public sealed record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;

public sealed class GetDashboardStatsHandler(
    IApplicationDbContext db,
    ICurrentStore currentStore)
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();

        // 1. Lấy thông tin khách hàng và sản phẩm của shop
        var totalCustomers = await db.Customers.CountAsync(c => c.StoreId == storeId, ct);
        var totalProducts = await db.Products.CountAsync(p => p.StoreId == storeId, ct);

        // 2. Sản phẩm có tồn kho thấp (<= 5)
        var lowStockEntities = await db.Products
            .Where(p => p.StoreId == storeId && p.StockQuantity <= 5 && p.IsActive)
            .OrderBy(p => p.StockQuantity)
            .Take(10)
            .ToListAsync(ct);

        var lowStockCount = await db.Products
            .CountAsync(p => p.StoreId == storeId && p.StockQuantity <= 5 && p.IsActive, ct);

        var lowStockDtos = lowStockEntities.Select(p => new LowStockProductDto(
            p.Id.Value,
            p.Name.Value,
            p.Sku,
            p.StockQuantity,
            p.Price.Amount,
            p.Price.Currency
        )).ToList();

        // 3. Tính khoảng thời gian "Hôm nay" (theo múi giờ GMT+7)
        var nowUtc = DateTime.UtcNow;
        var vnOffset = TimeSpan.FromHours(7);
        var vnTodayDate = (nowUtc + vnOffset).Date;
        var todayStartUtc = vnTodayDate - vnOffset;
        var todayEndUtc = todayStartUtc.AddDays(1);

        var todayCustomers = await db.Customers
            .CountAsync(c => c.StoreId == storeId && c.CreatedAt >= todayStartUtc && c.CreatedAt < todayEndUtc, ct);

        // 4. Thống kê đơn hàng
        var orders = await db.Orders
            .Where(o => o.StoreId == storeId)
            .ToListAsync(ct);

        var totalOrders = orders.Count;
        var pendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
        var confirmedOrders = orders.Count(o => o.Status == OrderStatus.Confirmed);
        var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
        var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

        // Doanh thu tính trên các đơn Confirmed hoặc Completed
        var validOrders = orders.Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Completed).ToList();
        var totalRevenue = validOrders.Sum(o => o.TotalAmount.Amount);
        var currency = validOrders.FirstOrDefault()?.TotalAmount.Currency ?? "VND";

        // Thống kê riêng trong ngày hôm nay
        var todayOrders = orders.Count(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc);
        var todayRevenue = validOrders
            .Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc)
            .Sum(o => o.TotalAmount.Amount);

        // 5. Lấy 5 đơn hàng mới nhất kèm tên khách hàng
        var recentOrdersRaw = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToList();

        var customerIds = recentOrdersRaw.Select(o => o.CustomerId).Distinct().ToList();
        var customerMap = await db.Customers
            .Where(c => c.StoreId == storeId && customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var recentOrderDtos = recentOrdersRaw.Select(o => new RecentOrderDto(
            o.Id.Value,
            o.Code,
            customerMap.GetValueOrDefault(o.CustomerId, "Khách vãng lai"),
            o.TotalAmount.Amount,
            o.TotalAmount.Currency,
            o.Status.ToString(),
            o.CreatedAt
        )).ToList();

        var stats = new DashboardStatsDto(
            TodayRevenue: todayRevenue,
            TodayOrders: todayOrders,
            TodayCustomers: todayCustomers,
            TotalRevenue: totalRevenue,
            Currency: currency,
            TotalOrders: totalOrders,
            PendingOrders: pendingOrders,
            ConfirmedOrders: confirmedOrders,
            CompletedOrders: completedOrders,
            CancelledOrders: cancelledOrders,
            TotalCustomers: totalCustomers,
            TotalProducts: totalProducts,
            LowStockProductsCount: lowStockCount,
            LowStockProducts: lowStockDtos,
            RecentOrders: recentOrderDtos
        );

        return Result<DashboardStatsDto>.Success(stats);
    }
}
