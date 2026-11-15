using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class GoogleSheetConnection : AggregateRoot<GoogleSheetConnectionId>
{
    public StoreId StoreId { get; private set; }
    public string SpreadsheetId { get; private set; } = string.Empty;
    public string SheetName { get; private set; } = "Đơn hàng";
    public string EncryptedRefreshToken { get; private set; } = string.Empty;
    public GoogleSheetConnectionStatus Status { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private GoogleSheetConnection() { }

    public static GoogleSheetConnection Create(StoreId storeId) => new()
    {
        Id = GoogleSheetConnectionId.New(),
        StoreId = storeId,
        SpreadsheetId = string.Empty,
        SheetName = "Đơn hàng",
        EncryptedRefreshToken = string.Empty,
        Status = GoogleSheetConnectionStatus.Disconnected,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>Lưu refresh token (đã mã hóa) sau khi chủ cửa hàng cấp quyền Google OAuth.</summary>
    public void Authorize(string encryptedRefreshToken)
    {
        EncryptedRefreshToken = encryptedRefreshToken;
        Status = GoogleSheetConnectionStatus.Connected;
        LastErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConfig(string spreadsheetId, string sheetName)
    {
        SpreadsheetId = NormalizeSpreadsheetId(spreadsheetId);
        SheetName = string.IsNullOrWhiteSpace(sheetName) ? "Đơn hàng" : sheetName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Chấp nhận cả link Google Sheets lẫn ID; luôn lưu ID.</summary>
    public static string NormalizeSpreadsheetId(string input)
    {
        var value = input.Trim();
        var match = System.Text.RegularExpressions.Regex.Match(value, @"/spreadsheets/d/([a-zA-Z0-9_-]+)");
        return match.Success ? match.Groups[1].Value : value;
    }

    public void RecordSyncSuccess()
    {
        Status = GoogleSheetConnectionStatus.Connected;
        LastSyncAt = DateTime.UtcNow;
        LastErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSyncFailure(string error)
    {
        Status = GoogleSheetConnectionStatus.SyncError;
        LastErrorMessage = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disconnect()
    {
        Status = GoogleSheetConnectionStatus.Disconnected;
        EncryptedRefreshToken = string.Empty;
        LastErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
