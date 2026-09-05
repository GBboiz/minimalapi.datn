using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.GetStatus;

public sealed record GetGoogleSheetStatusQuery : IRequest<Result<GoogleSheetStatusDto>>;

public sealed class GetGoogleSheetStatusHandler(
    IGoogleSheetConnectionRepository connectionRepo,
    ICurrentStore currentStore)
    : IRequestHandler<GetGoogleSheetStatusQuery, Result<GoogleSheetStatusDto>>
{
    public async Task<Result<GoogleSheetStatusDto>> Handle(GetGoogleSheetStatusQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var connection = await connectionRepo.GetByStoreIdAsync(storeId, ct);

        if (connection is null || connection.Status == GoogleSheetConnectionStatus.Disconnected)
        {
            return Result<GoogleSheetStatusDto>.Success(new GoogleSheetStatusDto(
                IsConnected: false,
                Status: "Disconnected",
                SpreadsheetId: string.Empty,
                SheetName: "Đơn hàng",
                LastSyncAt: null,
                LastErrorMessage: null
            ));
        }

        return Result<GoogleSheetStatusDto>.Success(new GoogleSheetStatusDto(
            IsConnected: true,
            Status: connection.Status.ToString(),
            SpreadsheetId: connection.SpreadsheetId,
            SheetName: connection.SheetName,
            LastSyncAt: connection.LastSyncAt,
            LastErrorMessage: connection.LastErrorMessage
        ));
    }
}
