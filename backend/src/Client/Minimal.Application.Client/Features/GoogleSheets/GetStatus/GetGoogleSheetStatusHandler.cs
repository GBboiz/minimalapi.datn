using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.GetStatus;

public sealed record GetGoogleSheetStatusQuery : IRequest<Result<GoogleSheetStatusDto>>;

public sealed class GetGoogleSheetStatusHandler(
    IGoogleSheetConnectionRepository connectionRepo,
    IGoogleSheetsService googleSheetsService,
    ICurrentStore currentStore)
    : IRequestHandler<GetGoogleSheetStatusQuery, Result<GoogleSheetStatusDto>>
{
    public async Task<Result<GoogleSheetStatusDto>> Handle(GetGoogleSheetStatusQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var connection = await connectionRepo.GetByStoreIdAsync(storeId, ct);

        if (connection is null)
        {
            return Result<GoogleSheetStatusDto>.Success(new GoogleSheetStatusDto(
                IsConnected: false,
                Status: "Disconnected",
                SpreadsheetId: string.Empty,
                SheetName: "Đơn hàng",
                LastSyncAt: null,
                LastErrorMessage: null,
                IsMockMode: googleSheetsService.IsMockMode
            ));
        }

        // Trả về cấu hình đã lưu kể cả khi chưa cấp quyền, để form giữ giá trị sau khi quay lại từ Google
        return Result<GoogleSheetStatusDto>.Success(new GoogleSheetStatusDto(
            IsConnected: connection.CanSync,
            Status: connection.Status.ToString(),
            SpreadsheetId: connection.SpreadsheetId,
            SheetName: connection.SheetName,
            LastSyncAt: connection.LastSyncAt,
            LastErrorMessage: connection.LastErrorMessage,
            IsMockMode: googleSheetsService.IsMockMode
        ));
    }
}
