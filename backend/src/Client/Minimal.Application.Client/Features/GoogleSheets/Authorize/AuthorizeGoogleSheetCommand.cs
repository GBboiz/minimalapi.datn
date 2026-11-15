using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.GoogleSheets.Authorize;

/// <summary>
/// Lưu refresh token sau khi Google redirect về callback.
/// StoreId lấy từ OAuth state đã ký (callback không có JWT nên không dùng ICurrentStore).
/// </summary>
public sealed record AuthorizeGoogleSheetCommand(
    StoreId StoreId,
    string RefreshToken
) : IRequest<Result<bool>>;
