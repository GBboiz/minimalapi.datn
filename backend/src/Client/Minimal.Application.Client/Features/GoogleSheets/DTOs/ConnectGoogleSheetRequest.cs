namespace MinimalAPI.Application.Features.GoogleSheets.DTOs;

public sealed record ConnectGoogleSheetRequest(
    string SpreadsheetId,
    string SheetName,
    string? RefreshToken = null
);
