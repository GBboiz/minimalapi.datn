namespace MinimalAPI.Application.Features.GoogleSheets.DTOs;

public sealed record SaveGoogleSheetConfigRequest(
    string SpreadsheetId,
    string SheetName
);
