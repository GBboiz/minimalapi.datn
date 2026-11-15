namespace MinimalAPI.Domain.Entities;

public enum GoogleSheetConnectionStatus
{
    Disconnected = 0,
    Connected = 1,
    SyncError = 2,
    /// <summary>Google đã thu hồi/hết hạn quyền — chủ cửa hàng cần kết nối lại.</summary>
    ReauthorizationRequired = 3
}
