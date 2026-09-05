using MediatR;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.GetOrder;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto?>;
