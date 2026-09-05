namespace MinimalAPI.Application.Features.GoogleSheets.DTOs;

public sealed record OrderSyncPayload(
    Guid OrderId,
    string Code,
    string CustomerName,
    string CustomerPhone,
    string ProductsSummary,
    int TotalQuantity,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTime CreatedAt
);
