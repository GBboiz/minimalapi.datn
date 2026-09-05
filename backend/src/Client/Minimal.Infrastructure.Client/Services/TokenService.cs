using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Services;

public sealed class TokenService(IConfiguration configuration)
{
    public static bool IsAdminUser(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var lower = email.Trim().ToLowerInvariant();
        return lower == "superadmin" || lower == "superadmin@minimalapi.local";
    }

    public string Create(User user, Store store, bool? isAdminOverride = null)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");
        var isAdmin = isAdminOverride ?? IsAdminUser(user.Email);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("store_id", store.Id.Value.ToString()),
            new Claim("store_name", store.Name),
            new Claim("is_admin", isAdmin ? "true" : "false"),
            new Claim(ClaimTypes.Role, isAdmin ? "SuperAdmin" : "StoreUser")
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"], audience: configuration["Jwt:Audience"], claims: claims,
            expires: expires, signingCredentials: credentials));
    }
}
