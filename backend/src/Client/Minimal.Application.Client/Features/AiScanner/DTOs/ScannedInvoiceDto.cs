namespace MinimalAPI.Application.Features.AiScanner.DTOs;

public sealed record ScannedInvoiceDto(
    string CustomerName,
    string Phone,
    string Address,
    string? InvoiceCode,
    List<ScannedInvoiceItemDto> Items,
    decimal TotalAmount,
    double AiConfidence,
    string AiSource
);

public sealed record ScannedInvoiceItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    Guid? MatchedProductId,
    string? MatchedProductSku,
    int CurrentStock,
    bool IsMatched
);

public sealed record ConfirmScannedOrderRequest(
    string CustomerName,
    string Phone,
    string Address,
    List<ConfirmScannedOrderItem> Items
);

public sealed record ConfirmScannedOrderItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public sealed record ConfirmScannedOrderResponse(
    Guid OrderId,
    string OrderCode,
    decimal TotalAmount,
    string Status,
    string CustomerName,
    bool SyncedToOutbox
);
