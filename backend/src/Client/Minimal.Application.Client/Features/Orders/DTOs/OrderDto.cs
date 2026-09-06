namespace MinimalAPI.Application.Features.Orders.DTOs;

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal TotalPrice,
    bool IsGift = false);

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerAddress,
    string Code,
    string Status,
    decimal SubTotal,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public record OrderSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string Code,
    string Status,
    decimal SubTotal,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Currency,
    int TotalItems,
    DateTime CreatedAt);
