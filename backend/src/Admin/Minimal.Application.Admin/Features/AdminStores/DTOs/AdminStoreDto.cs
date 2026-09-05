namespace MinimalAPI.Application.Features.AdminStores.DTOs;

public sealed record AdminStoreDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    int TotalProducts,
    int TotalOrders,
    decimal TotalRevenue,
    string Currency,
    int TotalCustomers,
    int TotalMembers
);

public sealed record CreateStoreAdminRequest(
    string Name,
    string OwnerFullName,
    string OwnerEmail,
    string Password
);

public sealed record UpdateStoreAdminRequest(
    string Name,
    string? Slug,
    string? OwnerFullName
);

public sealed record SwitchStoreResponse(
    string AccessToken,
    Guid StoreId,
    string StoreName
);
