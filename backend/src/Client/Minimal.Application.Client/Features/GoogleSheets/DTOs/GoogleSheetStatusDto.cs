namespace MinimalAPI.Application.Features.GoogleSheets.DTOs;

public sealed record GoogleSheetStatusDto(
    bool IsConnected,
    string Status,
    string SpreadsheetId,
    string SheetName,
    DateTime? LastSyncAt,
    string? LastErrorMessage
);
