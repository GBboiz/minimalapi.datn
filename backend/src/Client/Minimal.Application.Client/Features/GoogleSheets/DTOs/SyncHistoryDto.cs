namespace MinimalAPI.Application.Features.GoogleSheets.DTOs;

public sealed record SyncHistoryDto(
    Guid Id,
    string Type,
    string OrderCode,
    string CustomerName,
    string ProductsSummary,
    decimal TotalAmount,
    string Currency,
    string Status, // Success, Pending, Failed
    int RetryCount,
    string? Error,
    DateTime OccurredAt,
    DateTime? ProcessedAt
);
