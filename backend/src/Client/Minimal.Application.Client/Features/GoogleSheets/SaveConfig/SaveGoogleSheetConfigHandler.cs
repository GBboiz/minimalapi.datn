using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.SaveConfig;

public sealed class SaveGoogleSheetConfigHandler(
    IGoogleSheetConnectionRepository connectionRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SaveGoogleSheetConfigCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SaveGoogleSheetConfigCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var connection = await connectionRepo.GetByStoreIdAsync(storeId, ct);

        if (connection is null)
        {
            connection = GoogleSheetConnection.Create(storeId);
            connection.UpdateConfig(request.SpreadsheetId, request.SheetName);
            connectionRepo.Add(connection);
        }
        else
        {
            // Giữ nguyên refresh token & trạng thái hiện có — chỉ đổi cấu hình bảng tính
            connection.UpdateConfig(request.SpreadsheetId, request.SheetName);
            connectionRepo.Update(connection);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
