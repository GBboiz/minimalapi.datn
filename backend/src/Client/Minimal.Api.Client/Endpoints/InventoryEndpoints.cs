using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Features.Inventory.AdjustInventory;
using MinimalAPI.Application.Features.Inventory.DTOs;
using MinimalAPI.Application.Features.Inventory.GetInventory;

namespace MinimalAPI.Api.Endpoints;

public static class InventoryEndpoints
{
    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .RequireAuthorization();

        group.MapGet("/", async (
            ISender sender,
            string? search = null,
            Guid? categoryId = null,
            bool? lowStockOnly = null) =>
        {
            var result = await sender.Send(new GetInventoryQuery(search, categoryId, lowStockOnly));
            return TypedResults.Ok(result);
        })
        .WithName("GetInventory")
        .WithSummary("Lấy danh sách và thống kê tồn kho (thực tế và dự báo)")
        .Produces<InventorySummaryDto>();

        group.MapPost("/adjust", async Task<IResult> (AdjustInventoryCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Ok(new { stockQuantity = result.Value })
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("AdjustInventory")
        .WithSummary("Điều chỉnh số lượng tồn kho thực tế (nhập/xuất kho)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }
}
