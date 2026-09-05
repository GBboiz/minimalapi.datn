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

        foreach (var message in messages)
        {
            if (ct.IsCancellationRequested) break;

            var connection = await connectionRepo.GetByStoreIdAsync(message.StoreId, ct);
            if (connection is null || connection.Status == GoogleSheetConnectionStatus.Disconnected || string.IsNullOrEmpty(connection.EncryptedRefreshToken))
            {
                _logger.LogDebug("[GoogleSheetsSyncWorker] Cửa hàng {StoreId} chưa kích hoạt kết nối Google Sheets. Tạm bỏ qua.", message.StoreId);
                continue;
            }

            try
            {
                var refreshToken = encryptionService.Decrypt(connection.EncryptedRefreshToken);
                var payload = JsonSerializer.Deserialize<OrderSyncPayload>(message.Payload);
                if (payload is null)
                {
                    message.RecordFailure("Payload không đúng định dạng.");
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
            catch (Exception ex)
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
