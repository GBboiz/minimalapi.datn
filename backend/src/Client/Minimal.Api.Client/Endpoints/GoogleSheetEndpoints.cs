using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.Connect;
using MinimalAPI.Application.Features.GoogleSheets.Disconnect;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Application.Features.GoogleSheets.GetHistory;
using MinimalAPI.Application.Features.GoogleSheets.GetStatus;
using MinimalAPI.Application.Features.GoogleSheets.Retry;
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
        group.MapGet("/connect-url", (IGoogleSheetsService googleService, ICurrentStore currentStore) =>
        {
            var storeId = currentStore.GetRequiredStoreId();
            var url = googleService.GetAuthorizationUrl(storeId.ToString());
            return Results.Ok(new { url });
        })
        .WithName("GetGoogleOAuthUrl")
        .WithSummary("Lấy URL điều hướng cấp quyền Google OAuth.");

        // 3. Kết nối / Lưu cấu hình Spreadsheet
        group.MapPost("/connect", async (ConnectGoogleSheetRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ConnectGoogleSheetCommand(
                request.SpreadsheetId,
                request.SheetName,
                request.RefreshToken));

            return result.IsSuccess ? Results.Ok(new { message = "Kết nối Google Sheets thành công." }) : Results.BadRequest(result.Error);
        })
        .WithName("ConnectGoogleSheet")
        .WithSummary("Kết nối hoặc lưu cấu hình bảng tính Google Sheets.");

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
            ISender sender,
            IConfiguration config) =>
        {
            var frontendUrl = config["Frontend:Url"] ?? "http://localhost:4201";

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Results.Redirect($"{frontendUrl}/google-sheets?error=" + Uri.EscapeDataString(error ?? "Cấp quyền bị từ chối"));
            }

            try
            {
                var refreshToken = await googleService.ExchangeCodeForRefreshTokenAsync(code);
                if (Guid.TryParse(state, out var storeGuid))
                {
                    // Lưu refresh token vào store
                    await sender.Send(new ConnectGoogleSheetCommand("SPREADSHEET_ID_HERE", "Đơn hàng", refreshToken));
                }

                return Results.Redirect($"{frontendUrl}/google-sheets?connected=true");
            }
            catch (Exception ex)
            {
                return Results.Redirect($"{frontendUrl}/google-sheets?error=" + Uri.EscapeDataString(ex.Message));
            }
        })
        .AllowAnonymous()
        .WithTags("Google Sheets")
        .WithName("GoogleOAuthCallback")
        .WithSummary("Callback xử lý phản hồi từ Google OAuth 2.0.");

        return group;
    }
}
