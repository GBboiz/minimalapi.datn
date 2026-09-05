using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.GoogleSheets.Disconnect;

public sealed record DisconnectGoogleSheetCommand : IRequest<Result<bool>>;
