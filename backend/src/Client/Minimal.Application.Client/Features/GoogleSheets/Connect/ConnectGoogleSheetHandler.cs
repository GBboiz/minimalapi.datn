using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.Connect;

public sealed class ConnectGoogleSheetHandler(
    IGoogleSheetConnectionRepository connectionRepo,
    IEncryptionService encryptionService,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConnectGoogleSheetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ConnectGoogleSheetCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();

        if (string.IsNullOrWhiteSpace(request.SpreadsheetId))
            return Result<bool>.Failure("Spreadsheet ID không được để trống.");

        var connection = await connectionRepo.GetByStoreIdAsync(storeId, ct);
        var tokenToEncrypt = string.IsNullOrWhiteSpace(request.RefreshToken)
            ? $"demo_refresh_token_{storeId.Value:N}"
            : request.RefreshToken;

        var encryptedToken = encryptionService.Encrypt(tokenToEncrypt);

        if (connection is null)
        {
            connection = GoogleSheetConnection.Create(storeId);
            connection.Connect(request.SpreadsheetId, request.SheetName, encryptedToken);
            connectionRepo.Add(connection);
        }
        else
        {
            connection.Connect(request.SpreadsheetId, request.SheetName, encryptedToken);
            connectionRepo.Update(connection);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
