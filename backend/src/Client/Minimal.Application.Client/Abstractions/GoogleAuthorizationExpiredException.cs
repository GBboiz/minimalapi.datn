namespace MinimalAPI.Application.Abstractions;

/// <summary>
/// Google từ chối refresh token (invalid_grant: hết hạn, bị thu hồi...).
/// Không phải lỗi tạm thời — thử lại vô ích, cửa hàng phải kết nối lại.
/// </summary>
public sealed class GoogleAuthorizationExpiredException(string message) : Exception(message);
