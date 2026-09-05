using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.GoogleSheets.Connect;

public sealed record ConnectGoogleSheetCommand(
    string SpreadsheetId,
    string SheetName,
    string? RefreshToken = null
) : IRequest<Result<bool>>;
