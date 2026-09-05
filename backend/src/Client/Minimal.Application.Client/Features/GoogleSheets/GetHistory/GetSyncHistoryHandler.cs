using System.Text.Json;
using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.GoogleSheets.GetHistory;

public sealed record GetSyncHistoryQuery(int Count = 20) : IRequest<Result<List<SyncHistoryDto>>>;

public sealed class GetSyncHistoryHandler(
    IOutboxRepository outboxRepo,
    ICurrentStore currentStore)
    : IRequestHandler<GetSyncHistoryQuery, Result<List<SyncHistoryDto>>>
{
    public async Task<Result<List<SyncHistoryDto>>> Handle(GetSyncHistoryQuery request, CancellationToken ct)
    {
        var storeId = currentStore.GetRequiredStoreId();
        var messages = await outboxRepo.GetRecentByStoreIdAsync(storeId, request.Count, ct);

        var list = new List<SyncHistoryDto>();

        foreach (var msg in messages)
        {
            OrderSyncPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<OrderSyncPayload>(msg.Payload);
            }
            catch
            {
                // ignore parsing error for corrupted payload
            }

            var status = msg.ProcessedAt.HasValue
                ? "Success"
                : (!string.IsNullOrEmpty(msg.Error) && msg.RetryCount >= 5)
                    ? "Failed"
                    : (!string.IsNullOrEmpty(msg.Error))
                        ? "Retrying"
                        : "Pending";

            list.Add(new SyncHistoryDto(
                Id: msg.Id,
                Type: msg.Type,
                OrderCode: payload?.Code ?? "N/A",
                CustomerName: payload?.CustomerName ?? "N/A",
                ProductsSummary: payload?.ProductsSummary ?? "N/A",
                TotalAmount: payload?.TotalAmount ?? 0,
                Currency: payload?.Currency ?? "VND",
                Status: status,
                RetryCount: msg.RetryCount,
                Error: msg.Error,
                OccurredAt: msg.OccurredAt,
                ProcessedAt: msg.ProcessedAt
            ));
        }

        return Result<List<SyncHistoryDto>>.Success(list);
    }
}
