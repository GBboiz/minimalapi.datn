using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Inventory.AdjustInventory;

public record AdjustInventoryCommand(
    Guid ProductId,
    int StockChange, // Số lượng cộng thêm hoặc trừ bớt (ví dụ: +10 nhập hàng, -2 hao hụt)
    string? Reason = null) : IRequest<Result<int>>;
