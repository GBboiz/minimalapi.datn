using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Infrastructure.Persistence;
using MinimalAPI.Infrastructure.Services;

namespace MinimalAPI.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");
        group.MapPost("/register", async (RegisterRequest request, AppDbContext db, PasswordService passwords, TokenService tokens, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.StoreName))
                return Results.BadRequest(new { error = "Email, mật khẩu và tên cửa hàng là bắt buộc." });
            if (request.Password.Length < 8) return Results.BadRequest(new { error = "Mật khẩu cần ít nhất 8 ký tự." });
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(x => x.Email == email, ct)) return Results.BadRequest(new { error = "Email đã được sử dụng." });
            var user = User.Create(email, request.FullName, passwords.Hash(request.Password));
            var slug = ToSlug(request.StoreName);
            if (await db.Stores.AnyAsync(x => x.Slug == slug, ct)) slug = $"{slug}-{Guid.NewGuid():N}"[..8];
            var store = Store.Create(user.Id, request.StoreName, slug);
            var member = StoreMember.Create(store.Id, user.Id, StoreRole.Owner);
            db.Users.Add(user);
            db.Stores.Add(store);
            db.StoreMembers.Add(member);
            await db.SaveChangesAsync(ct);
            var isAdmin = TokenService.IsAdminUser(user.Email);
            return Results.Created("/api/stores/current", new AuthResponse(tokens.Create(user, store, isAdmin), store.Id.Value, store.Name, user.Email, user.FullName, isAdmin));
        });
        group.MapPost("/login", async (LoginRequest request, AppDbContext db, PasswordService passwords, TokenService tokens, CancellationToken ct) =>
        {
            var input = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(x =>
                x.Email == input ||
                (input == "superadmin" && x.Email == "superadmin@minimalapi.local"), ct);

            if (user is null || !passwords.Verify(request.Password, user.PasswordHash)) return Results.Unauthorized();

            var store = await db.Stores.FirstOrDefaultAsync(x => x.OwnerId == user.Id, ct);
            if (store is null)
            {
                var membership = await db.StoreMembers.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
                if (membership is not null)
                {
                    store = await db.Stores.FirstOrDefaultAsync(x => x.Id == membership.StoreId, ct);
                }
            }
            store ??= await db.Stores.FirstOrDefaultAsync(ct);

            if (store is null) return Results.BadRequest(new { error = "Hệ thống chưa có cửa hàng nào được khởi tạo." });

            var isAdmin = TokenService.IsAdminUser(user.Email);
            var token = tokens.Create(user, store, isAdmin);
            return Results.Ok(new AuthResponse(token, store.Id.Value, store.Name, user.Email, user.FullName, isAdmin));
        });
        return app;
    }

    private static string ToSlug(string value) => string.Concat(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
    public sealed record RegisterRequest(string Email, string FullName, string Password, string StoreName);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record AuthResponse(string AccessToken, Guid StoreId, string StoreName, string Email, string FullName, bool IsAdmin);
}
