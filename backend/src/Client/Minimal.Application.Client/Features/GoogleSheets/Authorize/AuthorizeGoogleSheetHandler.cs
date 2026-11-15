using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.Authorize;

public sealed class AuthorizeGoogleSheetHandler(
    IGoogleSheetConnectionRepository connectionRepo,
    IEncryptionService encryptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AuthorizeGoogleSheetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AuthorizeGoogleSheetCommand request, CancellationToken ct)
    {
        var connection = await connectionRepo.GetByStoreIdAsync(request.StoreId, ct);
        if (connection is null || string.IsNullOrWhiteSpace(connection.SpreadsheetId))
            return Result<bool>.Failure("Vui lòng lưu cấu hình bảng tính trước khi cấp quyền Google.");

        connection.Authorize(encryptionService.Encrypt(request.RefreshToken));
        connectionRepo.Update(connection);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
