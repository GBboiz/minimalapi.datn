using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Api.Services;

public sealed class CurrentStore(IHttpContextAccessor httpContextAccessor) : ICurrentStore
{
    public StoreId GetRequiredStoreId()
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue("store_id");
        return Guid.TryParse(value, out var storeId)
            ? new StoreId(storeId)
            : throw new UnauthorizedAccessException("Token không chứa cửa hàng hợp lệ.");
    }
}
