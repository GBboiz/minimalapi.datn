using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;

namespace MinimalAPI.Infrastructure.Services;

public sealed class GoogleSheetsService : IGoogleSheetsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSheetsService> _logger;

    public GoogleSheetsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleSheetsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string? ClientId => _configuration["Google:ClientId"];
    private string? ClientSecret => _configuration["Google:ClientSecret"];
    private string RedirectUri => _configuration["Google:RedirectUri"] ?? "http://localhost:5001/api/googlesheets/callback";
    public bool IsMockMode => string.IsNullOrWhiteSpace(ClientId) || ClientId.StartsWith("mock", StringComparison.OrdinalIgnoreCase) || ClientId.StartsWith("demo", StringComparison.OrdinalIgnoreCase);

    public string GetAuthorizationUrl(string state)
    {
        if (IsMockMode)
        {
            // Trong chế độ Mock, redirect trực tiếp về callback với mã demo
            return $"{RedirectUri}?code=mock_auth_code_{Guid.NewGuid():N}&state={Uri.EscapeDataString(state)}";
        }

        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/spreadsheets");
        var encodedRedirect = Uri.EscapeDataString(RedirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={ClientId}&" +
               $"redirect_uri={encodedRedirect}&" +
               $"response_type=code&" +
               $"scope={scope}&" +
               $"access_type=offline&" +
               $"prompt=consent&" +
               $"state={encodedState}";
    }

    public async Task<string> ExchangeCodeForRefreshTokenAsync(string code, CancellationToken ct = default)
    {
        if (IsMockMode || code.StartsWith("mock_", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[GoogleSheets] Chế độ Mock: Kích hoạt refresh token demo thành công.");
            return $"demo_refresh_token_{Guid.NewGuid():N}";
        }

        var postData = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = ClientId!,
            ["client_secret"] = ClientSecret!,
            ["redirect_uri"] = RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        using var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(postData), ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[GoogleSheets] Lỗi đổi mã xác thực Google: {Error}", err);
            throw new InvalidOperationException($"Lỗi kết nối Google OAuth: {response.StatusCode} - {err}");
        }

        var tokenRes = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct);
        if (string.IsNullOrEmpty(tokenRes?.RefreshToken))
        {
            throw new InvalidOperationException("Google không trả về Refresh Token. Hãy cấp lại quyền consent.");
        }

        return tokenRes.RefreshToken;
    }

    public async Task AppendOrderRowAsync(
        string spreadsheetId,
        string sheetName,
        string decryptedRefreshToken,
        OrderSyncPayload payload,
        CancellationToken ct = default)
    {
        if (!IsMockMode && decryptedRefreshToken.StartsWith("demo_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Cửa hàng đang dùng token demo. Vui lòng bấm 'Lưu cấu hình & Kết nối với Google' để cấp quyền thật.");

        if (IsMockMode)
        {
            _logger.LogInformation(
                "[GoogleSheets Mock] Đã đồng bộ đơn hàng {Code} vào Sheet '{Sheet}' (Spreadsheet ID: {Id}): Khách {Customer}, Tổng tiền {Total} {Currency}",
                payload.Code, sheetName, spreadsheetId, payload.CustomerName, payload.TotalAmount, payload.Currency);
            return;
        }

        // 1. Lấy Access Token từ Refresh Token
        var postData = new Dictionary<string, string>
        {
            ["refresh_token"] = decryptedRefreshToken,
            ["client_id"] = ClientId!,
            ["client_secret"] = ClientSecret!,
            ["grant_type"] = "refresh_token"
        };

        using var tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(postData), ct);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var err = await tokenResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Không thể làm mới Google Access Token: {err}");
        }

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct);
        var accessToken = tokenData?.AccessToken;

        // 2. Đảm bảo dòng tiêu đề, rồi ghi dòng vào Sheet qua Google Sheets API v4
        await EnsureHeaderRowAsync(spreadsheetId, sheetName, accessToken, ct);

        var range = $"{QuoteSheetName(sheetName)}!A:H";
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{Uri.EscapeDataString(range)}:append?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";

        var rowValues = new object[]
        {
            payload.Code,
            ToLocalTime(payload.CreatedAt).ToString("dd/MM/yyyy HH:mm"),
            payload.CustomerName,
            // Dấu ' buộc Google giữ nguyên dạng văn bản (không mất số 0 đầu)
            $"'{payload.CustomerPhone}",
            payload.ProductsSummary,
            payload.TotalQuantity,
            $"{payload.TotalAmount:N0} {payload.Currency}",
            payload.Status
        };

        var requestBody = new
        {
            values = new[] { rowValues }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var appendResponse = await _httpClient.SendAsync(request, ct);
        if (!appendResponse.IsSuccessStatusCode)
        {
            var err = await appendResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError("[GoogleSheets] Lỗi ghi dữ liệu vào Google Sheets: {Error}", err);
            throw new InvalidOperationException($"Lỗi ghi Google Sheet: {err}");
        }

        _logger.LogInformation("[GoogleSheets] Ghi thành công đơn {Code} vào Google Sheet {Id}", payload.Code, spreadsheetId);
    }

    private static readonly string[] HeaderRow =
        ["Mã đơn hàng", "Ngày tạo", "Khách hàng", "Số điện thoại", "Sản phẩm", "Số lượng", "Tổng tiền", "Trạng thái"];

    private static string QuoteSheetName(string sheetName) => $"'{sheetName.Replace("'", "''")}'";

    /// <summary>Đổi giờ UTC sang múi giờ cấu hình (mặc định giờ Việt Nam, UTC+7).</summary>
    private DateTime ToLocalTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        var timeZoneId = _configuration["Google:TimeZone"] ?? "Asia/Ho_Chi_Minh";
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return utc.AddHours(7);
        }
    }

    /// <summary>
    /// Nếu ô A1 chưa phải tiêu đề thì chèn 1 dòng trống lên đầu sheet và ghi tiêu đề vào đó
    /// (không ghi đè dữ liệu cũ).
    /// </summary>
    private async Task EnsureHeaderRowAsync(string spreadsheetId, string sheetName, string? accessToken, CancellationToken ct)
    {
        var baseUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}";
        var headerRange = Uri.EscapeDataString($"{QuoteSheetName(sheetName)}!A1:H1");

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/values/{headerRange}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var getResponse = await _httpClient.SendAsync(getRequest, ct);
        if (!getResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Lỗi đọc Google Sheet: {await getResponse.Content.ReadAsStringAsync(ct)}");

        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync(ct));
        var firstCell = doc.RootElement.TryGetProperty("values", out var values)
                        && values.GetArrayLength() > 0 && values[0].GetArrayLength() > 0
            ? values[0][0].GetString()
            : null;

        if (string.Equals(firstCell?.Trim(), HeaderRow[0], StringComparison.OrdinalIgnoreCase))
            return;

        // Sheet đã có dữ liệu nhưng chưa có tiêu đề → chèn dòng trống lên đầu
        if (!string.IsNullOrEmpty(firstCell))
        {
            var sheetId = await GetSheetIdAsync(baseUrl, sheetName, accessToken, ct);
            var insertBody = new
            {
                requests = new object[]
                {
                    new { insertDimension = new { range = new { sheetId, dimension = "ROWS", startIndex = 0, endIndex = 1 }, inheritFromBefore = false } }
                }
            };
            await SendAsync(HttpMethod.Post, $"{baseUrl}:batchUpdate", insertBody, accessToken, ct);
        }

        await SendAsync(HttpMethod.Put, $"{baseUrl}/values/{headerRange}?valueInputOption=RAW",
            new { values = new[] { HeaderRow } }, accessToken, ct);
    }

    private async Task<int> GetSheetIdAsync(string baseUrl, string sheetName, string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}?fields=sheets.properties(sheetId,title)");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Lỗi đọc Google Sheet: {await response.Content.ReadAsStringAsync(ct)}");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        foreach (var sheet in doc.RootElement.GetProperty("sheets").EnumerateArray())
        {
            var props = sheet.GetProperty("properties");
            if (props.GetProperty("title").GetString() == sheetName)
                return props.GetProperty("sheetId").GetInt32();
        }
        throw new InvalidOperationException($"Không tìm thấy tab '{sheetName}' trong Google Sheet.");
    }

    private async Task SendAsync(HttpMethod method, string url, object body, string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Lỗi ghi Google Sheet: {await response.Content.ReadAsStringAsync(ct)}");
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
