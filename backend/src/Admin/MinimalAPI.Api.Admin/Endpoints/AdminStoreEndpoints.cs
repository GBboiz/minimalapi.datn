using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Features.AdminStores.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Infrastructure.Persistence;
using MinimalAPI.Infrastructure.Services;

namespace MinimalAPI.Api.Admin.Endpoints;

public static class AdminStoreEndpoints
{
    public static WebApplication MapAdminStoreEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/stores")
            .WithTags("Admin Stores")
            .RequireAuthorization();

        // 1. Lấy danh sách tất cả các shop kèm thống kê
        group.MapGet("", async (HttpContext httpContext, AppDbContext db, CancellationToken ct) =>
        {
            if (!IsAuthorizedAdmin(httpContext))
            {
                return Results.Json(new { error = "Chỉ tài khoản Quản trị viên (superadmin) mới có quyền truy cập chức năng này." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var stores = await db.Stores.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
            var ownerIds = stores.Select(s => s.OwnerId).Distinct().ToList();

            var owners = await db.Users.Where(u => ownerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

            // Thống kê sản phẩm
            var productCounts = await db.Products
                .GroupBy(p => p.StoreId)
                .Select(g => new { StoreId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoreId, x => x.Count, ct);

            // Thống kê đơn hàng và doanh thu
            var orderStats = await db.Orders
                .GroupBy(o => o.StoreId)
                .Select(g => new
                {
                    StoreId = g.Key,
                    TotalOrders = g.Count(),
                    Revenue = g.Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Completed)
                               .Sum(o => o.TotalAmount.Amount)
                })
                .ToDictionaryAsync(x => x.StoreId, ct);

            // Thống kê khách hàng
            var customerCounts = await db.Customers
                .GroupBy(c => c.StoreId)
                .Select(g => new { StoreId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoreId, x => x.Count, ct);

            // Thống kê thành viên
            var memberCounts = await db.StoreMembers
                .GroupBy(m => m.StoreId)
                .Select(g => new { StoreId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoreId, x => x.Count, ct);

            var dtos = stores.Select(s =>
            {
                owners.TryGetValue(s.OwnerId, out var owner);
                orderStats.TryGetValue(s.Id, out var oStat);

                return new AdminStoreDto(
                    Id: s.Id.Value,
                    Name: s.Name,
                    Slug: s.Slug,
                    CreatedAt: s.CreatedAt,
                    OwnerId: s.OwnerId.Value,
                    OwnerName: owner?.FullName ?? "Chưa rõ",
                    OwnerEmail: owner?.Email ?? "Chưa rõ",
                    TotalProducts: productCounts.GetValueOrDefault(s.Id, 0),
                    TotalOrders: oStat?.TotalOrders ?? 0,
                    TotalRevenue: oStat?.Revenue ?? 0,
                    Currency: "VND",
                    TotalCustomers: customerCounts.GetValueOrDefault(s.Id, 0),
                    TotalMembers: memberCounts.GetValueOrDefault(s.Id, 1)
                );
            }).ToList();

            return Results.Ok(dtos);
        });

        // 2. Tạo shop mới
        group.MapPost("", async (CreateStoreAdminRequest request, HttpContext httpContext, AppDbContext db, PasswordService passwords, CancellationToken ct) =>
        {
            if (!IsAuthorizedAdmin(httpContext))
            {
                return Results.Json(new { error = "Chỉ tài khoản Quản trị viên (superadmin) mới có quyền truy cập chức năng này." }, statusCode: StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.OwnerEmail) || string.IsNullOrWhiteSpace(request.OwnerFullName))
            {
                return Results.BadRequest(new { error = "Tên cửa hàng, email và họ tên chủ sở hữu là bắt buộc." });
            }

            var email = request.OwnerEmail.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null)
            {
                var password = string.IsNullOrWhiteSpace(request.Password) ? "Shop@123456" : request.Password;
                user = User.Create(email, request.OwnerFullName, passwords.Hash(password));
                db.Users.Add(user);
            }

            var slug = ToSlug(request.Name);
            if (await db.Stores.AnyAsync(s => s.Slug == slug, ct))
            {
                slug = $"{slug}-{Guid.NewGuid():N}"[..8];
            }

            var store = Store.Create(user.Id, request.Name, slug);
            var member = StoreMember.Create(store.Id, user.Id, StoreRole.Owner);

            db.Stores.Add(store);
            db.StoreMembers.Add(member);
            await db.SaveChangesAsync(ct);

            var dto = new AdminStoreDto(
                Id: store.Id.Value,
                Name: store.Name,
                Slug: store.Slug,
                CreatedAt: store.CreatedAt,
                OwnerId: user.Id.Value,
                OwnerName: user.FullName,
                OwnerEmail: user.Email,
                TotalProducts: 0,
                TotalOrders: 0,
                TotalRevenue: 0,
                Currency: "VND",
                TotalCustomers: 0,
                TotalMembers: 1
            );

            return Results.Created($"/api/admin/stores/{store.Id.Value}", dto);
        });

        // 3. Sửa thông tin shop (Update)
        group.MapPut("/{id:guid}", async (Guid id, UpdateStoreAdminRequest request, HttpContext httpContext, AppDbContext db, CancellationToken ct) =>
        {
            if (!IsAuthorizedAdmin(httpContext))
            {
                return Results.Json(new { error = "Chỉ tài khoản Quản trị viên (superadmin) mới có quyền truy cập chức năng này." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == new StoreId(id), ct);
            if (store is null)
            {
                return Results.NotFound(new { error = "Không tìm thấy cửa hàng." });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Tên cửa hàng không được để trống." });
            }

            var slug = string.IsNullOrWhiteSpace(request.Slug) ? ToSlug(request.Name) : ToSlug(request.Slug);
            if (slug != store.Slug && await db.Stores.AnyAsync(s => s.Slug == slug && s.Id != store.Id, ct))
            {
                slug = $"{slug}-{Guid.NewGuid():N}"[..8];
            }

            store.Update(request.Name, slug);

            var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == store.OwnerId, ct);
            if (owner is not null && !string.IsNullOrWhiteSpace(request.OwnerFullName))
            {
                owner.UpdateFullName(request.OwnerFullName);
            }

            await db.SaveChangesAsync(ct);

            var productCount = await db.Products.CountAsync(p => p.StoreId == store.Id, ct);
            var orderCount = await db.Orders.CountAsync(o => o.StoreId == store.Id, ct);
            var revenue = await db.Orders
                .Where(o => o.StoreId == store.Id && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Completed))
                .SumAsync(o => o.TotalAmount.Amount, ct);
            var customerCount = await db.Customers.CountAsync(c => c.StoreId == store.Id, ct);
            var memberCount = await db.StoreMembers.CountAsync(m => m.StoreId == store.Id, ct);

            var dto = new AdminStoreDto(
                Id: store.Id.Value,
                Name: store.Name,
                Slug: store.Slug,
                CreatedAt: store.CreatedAt,
                OwnerId: store.OwnerId.Value,
                OwnerName: owner?.FullName ?? "Chưa rõ",
                OwnerEmail: owner?.Email ?? "Chưa rõ",
                TotalProducts: productCount,
                TotalOrders: orderCount,
                TotalRevenue: revenue,
                Currency: "VND",
                TotalCustomers: customerCount,
                TotalMembers: memberCount
            );

            return Results.Ok(dto);
        });

        // 4. Xóa shop (Delete)
        group.MapDelete("/{id:guid}", async (Guid id, HttpContext httpContext, AppDbContext db, CancellationToken ct) =>
        {
            if (!IsAuthorizedAdmin(httpContext))
            {
                return Results.Json(new { error = "Chỉ tài khoản Quản trị viên (superadmin) mới có quyền truy cập chức năng này." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == new StoreId(id), ct);
            if (store is null)
            {
                return Results.NotFound(new { error = "Không tìm thấy cửa hàng." });
            }

            var orderIds = await db.Orders.Where(o => o.StoreId == store.Id).Select(o => o.Id).ToListAsync(ct);
            var orderItems = await db.OrderItems.Where(oi => orderIds.Contains(oi.OrderId)).ToListAsync(ct);
            db.OrderItems.RemoveRange(orderItems);

            var orders = await db.Orders.Where(o => o.StoreId == store.Id).ToListAsync(ct);
            db.Orders.RemoveRange(orders);

            var products = await db.Products.Where(p => p.StoreId == store.Id).ToListAsync(ct);
            db.Products.RemoveRange(products);

            var categories = await db.Categories.Where(c => c.StoreId == store.Id).ToListAsync(ct);
            db.Categories.RemoveRange(categories);

            var customers = await db.Customers.Where(c => c.StoreId == store.Id).ToListAsync(ct);
            db.Customers.RemoveRange(customers);

            var outbox = await db.OutboxMessages.Where(m => m.StoreId == store.Id).ToListAsync(ct);
            db.OutboxMessages.RemoveRange(outbox);

            var sheets = await db.GoogleSheetConnections.Where(g => g.StoreId == store.Id).ToListAsync(ct);
            db.GoogleSheetConnections.RemoveRange(sheets);

            var members = await db.StoreMembers.Where(m => m.StoreId == store.Id).ToListAsync(ct);
            db.StoreMembers.RemoveRange(members);

            db.Stores.Remove(store);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        // 5. Chuyển ngữ cảnh sang shop được chọn (Switch Store)
        group.MapPost("/{id:guid}/switch", async (Guid id, HttpContext httpContext, AppDbContext db, TokenService tokens, CancellationToken ct) =>
        {
            if (!IsAuthorizedAdmin(httpContext))
            {
                return Results.Json(new { error = "Chỉ tài khoản Quản trị viên (superadmin) mới có quyền truy cập chức năng này." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == new StoreId(id), ct);
            if (store is null)
            {
                return Results.NotFound(new { error = "Không tìm thấy cửa hàng." });
            }

            var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = null;
            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userGuid))
            {
                user = await db.Users.FirstOrDefaultAsync(u => u.Id == new UserId(userGuid), ct);
            }

            user ??= await db.Users.FirstOrDefaultAsync(u => u.Id == store.OwnerId, ct);

            if (user is null)
            {
                return Results.BadRequest(new { error = "Không tìm thấy thông tin người dùng hợp lệ." });
            }

            var token = tokens.Create(user, store, isAdminOverride: true);
            return Results.Ok(new SwitchStoreResponse(token, store.Id.Value, store.Name));
        });

        return app;
    }

    private static bool IsAuthorizedAdmin(HttpContext context)
    {
        var isAdminClaim = context.User.FindFirst("is_admin")?.Value;
        if (isAdminClaim == "true") return true;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value 
                 ?? context.User.FindFirst("email")?.Value;
        return TokenService.IsAdminUser(email);
    }

    private static string ToSlug(string value) => string.Concat(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
}
