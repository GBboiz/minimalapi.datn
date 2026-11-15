using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Api.Services;

/// <summary>
/// Ký và mã hóa tham số <c>state</c> của Google OAuth (chứa StoreId + hạn dùng),
/// để callback ẩn danh biết đúng cửa hàng và chặn giả mạo (CSRF / gắn nhầm tài khoản Google).
/// </summary>
public sealed class GoogleOAuthStateProtector(IDataProtectionProvider dataProtectionProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector("MinimalAPI.GoogleSheets.OAuthState")
        .ToTimeLimitedDataProtector();

    public string Protect(StoreId storeId) =>
        _protector.Protect(storeId.Value.ToString("N"), Lifetime);

    public bool TryUnprotect(string state, out StoreId storeId)
    {
        storeId = default;
        try
        {
            var payload = _protector.Unprotect(state);
            if (!Guid.TryParseExact(payload, "N", out var value)) return false;

            storeId = new StoreId(value);
            return true;
        }
        catch (CryptographicException)
        {
            // State bị sửa, hết hạn, hoặc key Data Protection đã đổi
            return false;
        }
    }
}
