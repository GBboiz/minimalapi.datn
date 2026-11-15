namespace MinimalAPI.Application.Features.GoogleSheets.DTOs;

public sealed record GoogleSheetStatusDto(
    bool IsConnected,
    string Status,
    string SpreadsheetId,
    string SheetName,
    DateTime? LastSyncAt,
    string? LastErrorMessage,
    /// <summary>true khi server chưa cấu hình Google OAuth (Google:ClientId) — chỉ giả lập, không ghi Sheet thật.</summary>
    bool IsMockMode
);
