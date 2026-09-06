using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Products.SetGiftProduct;

public record SetGiftProductCommand(
    Guid ProductId,
    Guid? GiftProductId) : IRequest<Result<Guid>>;
