using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.GoogleSheets.SaveConfig;

/// <summary>Lưu cấu hình bảng tính (ID + tên tab). Refresh token chỉ được lưu qua luồng OAuth callback.</summary>
public sealed record SaveGoogleSheetConfigCommand(
    string SpreadsheetId,
    string SheetName
) : IRequest<Result<bool>>;
