using MinimalAPI.Application.Features.GoogleSheets.DTOs;

namespace MinimalAPI.Application.Abstractions;

public interface IGoogleSheetsService
{
    string GetAuthorizationUrl(string state);
    Task<string> ExchangeCodeForRefreshTokenAsync(string code, CancellationToken ct = default);
    Task AppendOrderRowAsync(
        string spreadsheetId,
        string sheetName,
        string decryptedRefreshToken,
        OrderSyncPayload payload,
        CancellationToken ct = default);
}
