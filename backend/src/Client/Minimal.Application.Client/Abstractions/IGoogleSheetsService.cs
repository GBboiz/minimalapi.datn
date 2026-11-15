using MinimalAPI.Application.Features.GoogleSheets.DTOs;

namespace MinimalAPI.Application.Abstractions;

public interface IGoogleSheetsService
{
    /// <summary>true khi chưa cấu hình Google:ClientId — chỉ giả lập, không gọi Google thật.</summary>
    bool IsMockMode { get; }

    string GetAuthorizationUrl(string state);
    Task<string> ExchangeCodeForRefreshTokenAsync(string code, CancellationToken ct = default);
    Task AppendOrderRowAsync(
        string spreadsheetId,
        string sheetName,
        string decryptedRefreshToken,
        OrderSyncPayload payload,
        CancellationToken ct = default);
}
