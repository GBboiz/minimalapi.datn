using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Api.Services;
using MinimalAPI.Application.Features.GoogleSheets.Authorize;
using MinimalAPI.Application.Features.GoogleSheets.Disconnect;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Application.Features.GoogleSheets.GetHistory;
using MinimalAPI.Application.Features.GoogleSheets.GetStatus;
using MinimalAPI.Application.Features.GoogleSheets.Retry;
using MinimalAPI.Application.Features.GoogleSheets.SaveConfig;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Api.Endpoints;

public static class GoogleSheetEndpoints
{
    public static RouteGroupBuilder MapGoogleSheetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/googlesheets")
            .WithTags("Google Sheets")
            .RequireAuthorization();

        // 1. Trạng thái kết nối
        group.MapGet("/status", async (ISender sender) =>
        {
            var result = await sender.Send(new GetGoogleSheetStatusQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetGoogleSheetStatus")
        .WithSummary("Lấy trạng thái kết nối Google Sheets của cửa hàng.");

        // 2. URL cấp quyền Google OAuth
        group.MapGet("/connect-url", (
            IGoogleSheetsService googleService,
            ICurrentStore currentStore,
            GoogleOAuthStateProtector stateProtector) =>
        {
            var state = stateProtector.Protect(currentStore.GetRequiredStoreId());
            return TypedResults.Ok(new { url = googleService.GetAuthorizationUrl(state) });
        })
        .WithName("GetGoogleOAuthUrl")
        .WithSummary("Lấy URL điều hướng cấp quyền Google OAuth.");

        // 3. Lưu cấu hình Spreadsheet (ID + tên tab). Refresh token chỉ nhận qua OAuth callback.
        group.MapPost("/connect", async Task<IResult> (SaveGoogleSheetConfigRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SaveGoogleSheetConfigCommand(
                request.SpreadsheetId,
                request.SheetName));

            return result.IsSuccess
                ? TypedResults.Ok(new { message = "Đã lưu cấu hình Google Sheets." })
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("SaveGoogleSheetConfig")
        .WithSummary("Lưu cấu hình bảng tính Google Sheets (bước 1, trước khi cấp quyền OAuth).");

        // 4. Hủy kết nối
        group.MapPost("/disconnect", async (ISender sender) =>
        {
            var result = await sender.Send(new DisconnectGoogleSheetCommand());
            return result.IsSuccess ? Results.Ok(new { message = "Đã ngắt kết nối Google Sheets." }) : Results.BadRequest(result.Error);
        })
        .WithName("DisconnectGoogleSheet")
        .WithSummary("Ngắt kết nối Google Sheets của cửa hàng.");

        // 5. Lịch sử đồng bộ Outbox
        group.MapGet("/history", async ([FromQuery] int count, ISender sender) =>
        {
            var result = await sender.Send(new GetSyncHistoryQuery(count > 0 ? count : 20));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetSyncHistory")
        .WithSummary("Lấy lịch sử tin nhắn đồng bộ Outbox gần nhất.");

        // 6. Thử lại tin nhắn lỗi
        group.MapPost("/retry/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new RetrySyncMessageCommand(id));
            return result.IsSuccess ? Results.Ok(new { message = "Đã kích hoạt thử lại đồng bộ." }) : Results.BadRequest(result.Error);
        })
        .WithName("RetrySyncMessage")
        .WithSummary("Thử lại đồng bộ đơn hàng bị lỗi.");

        // 7. OAuth callback endpoint (cho phép gọi ẩn danh từ Google redirect)
        app.MapGet("/api/googlesheets/callback", async (
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            IGoogleSheetsService googleService,
            GoogleOAuthStateProtector stateProtector,
            ISender sender,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var frontendUrl = config["Frontend:Url"] ?? "http://localhost:4201";
            RedirectHttpResult RedirectError(string message) =>
                TypedResults.Redirect($"{frontendUrl}/google-sheets?error=" + Uri.EscapeDataString(message));

            if (!string.IsNullOrEmpty(error))
                return RedirectError(error == "access_denied" ? "Bạn đã từ chối cấp quyền Google." : $"Google trả về lỗi: {error}");

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return RedirectError("Thiếu mã xác thực từ Google.");

            if (!stateProtector.TryUnprotect(state, out var storeId))
                return RedirectError("Phiên cấp quyền không hợp lệ hoặc đã hết hạn. Vui lòng kết nối lại.");

            try
            {
                var refreshToken = await googleService.ExchangeCodeForRefreshTokenAsync(code, ct);
                var result = await sender.Send(new AuthorizeGoogleSheetCommand(storeId, refreshToken), ct);

                return result.IsSuccess
                    ? TypedResults.Redirect($"{frontendUrl}/google-sheets?connected=true")
                    : RedirectError(result.Error!);
            }
            catch (InvalidOperationException ex)
            {
                loggerFactory.CreateLogger("GoogleSheetEndpoints")
                    .LogError(ex, "[GoogleSheets] OAuth callback thất bại cho cửa hàng {StoreId}", storeId.Value);
                return RedirectError("Không thể hoàn tất kết nối Google. Vui lòng thử lại.");
            }
        })
        .AllowAnonymous()
        .WithTags("Google Sheets")
        .WithName("GoogleOAuthCallback")
        .WithSummary("Callback xử lý phản hồi từ Google OAuth 2.0.");

        return group;
    }
}
