using MinimalAPI.Application.Features.AiScanner.DTOs;

namespace MinimalAPI.Application.Features.AiScanner.Interfaces;

public interface IAiVisionService
{
    Task<ScannedInvoiceDto> ExtractInvoiceFromImageAsync(
        byte[] imageBytes, 
        string contentType, 
        Guid storeId, 
        string? customApiKey = null,
        string? sampleType = null,
        CancellationToken ct = default);
}
