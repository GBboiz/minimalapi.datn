using MediatR;
using MinimalAPI.Application.Features.Inventory.DTOs;

namespace MinimalAPI.Application.Features.Inventory.GetInventory;

public record GetInventoryQuery(
    string? Search = null,
    Guid? CategoryId = null,
    bool? LowStockOnly = null) : IRequest<InventorySummaryDto>;
