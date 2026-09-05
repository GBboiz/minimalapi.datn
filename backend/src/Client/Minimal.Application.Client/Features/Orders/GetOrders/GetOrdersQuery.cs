using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.GetOrders;

public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Status = null) : IRequest<PagedResult<OrderSummaryDto>>;
