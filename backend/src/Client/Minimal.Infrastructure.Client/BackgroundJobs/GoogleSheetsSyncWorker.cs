using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.GoogleSheets.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.BackgroundJobs;

public sealed class GoogleSheetsSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GoogleSheetsSyncWorker> _logger;
    private readonly TimeSpan _period = TimeSpan.FromSeconds(10);

    public GoogleSheetsSyncWorker(
        IServiceProvider serviceProvider,
        ILogger<GoogleSheetsSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[GoogleSheetsSyncWorker] Background worker bắt đầu chạy.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[GoogleSheetsSyncWorker] Lỗi xảy ra trong vòng lặp worker.");
            }

            try
            {
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[GoogleSheetsSyncWorker] Background worker đã dừng.");
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var connectionRepo = scope.ServiceProvider.GetRequiredService<IGoogleSheetConnectionRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var googleSheetsService = scope.ServiceProvider.GetRequiredService<IGoogleSheetsService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var messages = await outboxRepo.GetUnprocessedMessagesAsync(batchSize: 10, ct);
        if (messages.Count == 0) return;

        _logger.LogInformation("[GoogleSheetsSyncWorker] Tìm thấy {Count} tin nhắn Outbox cần xử lý.", messages.Count);

        // Cửa hàng bị Google thu hồi quyền trong lượt này → bỏ qua các tin nhắn còn lại của cửa hàng đó
        var reauthRequiredStores = new HashSet<StoreId>();

        foreach (var message in messages)
        {
            if (ct.IsCancellationRequested) break;
            if (reauthRequiredStores.Contains(message.StoreId)) continue;

            var connection = await connectionRepo.GetByStoreIdAsync(message.StoreId, ct);
            if (connection is null || !connection.CanSync)
                continue; // Trạng thái vừa thay đổi sau khi truy vấn — để lượt sau xử lý

            try
            {
                var refreshToken = encryptionService.Decrypt(connection.EncryptedRefreshToken);
                var payload = JsonSerializer.Deserialize<OrderSyncPayload>(message.Payload);
                if (payload is null)
                {
                    message.RecordFailure("Payload không đúng định dạng.");
                    outboxRepo.Update(message);
                    await unitOfWork.SaveChangesAsync(ct);
                    continue;
                }

                await googleSheetsService.AppendOrderRowAsync(
                    connection.SpreadsheetId,
                    connection.SheetName,
                    refreshToken,
                    payload,
                    ct);

                message.MarkAsProcessed();
                connection.RecordSyncSuccess();
                outboxRepo.Update(message);
                connectionRepo.Update(connection);
                await unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("[GoogleSheetsSyncWorker] Đồng bộ thành công đơn hàng {Code} vào Google Sheets.", payload.Code);
            }
            catch (GoogleAuthorizationExpiredException ex)
            {
                // Không tăng RetryCount: đơn được giữ lại và tự đồng bộ bù khi cửa hàng kết nối lại
                _logger.LogWarning("[GoogleSheetsSyncWorker] Cửa hàng {StoreId} cần kết nối lại Google Sheets: {Reason}", message.StoreId.Value, ex.Message);
                reauthRequiredStores.Add(message.StoreId);
                connection.RequireReauthorization(ex.Message);
                connectionRepo.Update(connection);
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[GoogleSheetsSyncWorker] Lỗi đồng bộ tin nhắn Outbox {MessageId}: {Error}", message.Id, ex.Message);
                message.RecordFailure(ex.Message);
                connection.RecordSyncFailure(ex.Message);
                outboxRepo.Update(message);
                connectionRepo.Update(connection);
                await unitOfWork.SaveChangesAsync(ct);
            }
        }
    }
}
