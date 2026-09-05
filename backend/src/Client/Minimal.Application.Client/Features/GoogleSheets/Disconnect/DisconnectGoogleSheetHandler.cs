using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.Disconnect;

public sealed class DisconnectGoogleSheetHandler(
    IGoogleSheetConnectionRepository connectionRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DisconnectGoogleSheetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DisconnectGoogleSheetCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var connection = await connectionRepo.GetByStoreIdAsync(storeId, ct);

        if (connection is not null)
        {
            connection.Disconnect();
            connectionRepo.Update(connection);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result<bool>.Success(true);
    }
}
