using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.Retry;

public sealed record RetrySyncMessageCommand(Guid Id) : IRequest<Result<bool>>;

public sealed class RetrySyncMessageHandler(
    IOutboxRepository outboxRepo,
    ICurrentStore currentStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RetrySyncMessageCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RetrySyncMessageCommand request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var message = await outboxRepo.GetByIdAsync(request.Id, ct);

        if (message is null || message.StoreId != storeId)
            return Result<bool>.Failure("Không tìm thấy tin nhắn đồng bộ cần thử lại.");

        message.ResetForRetry();
        outboxRepo.Update(message);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
