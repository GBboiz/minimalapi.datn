using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.CancelOrder;
using MinimalAPI.Application.Features.Orders.CompleteOrder;
using MinimalAPI.Application.Features.Orders.ConfirmOrder;
using MinimalAPI.Application.Features.Orders.CreateOrder;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Application.Features.Orders.GetOrder;
using MinimalAPI.Application.Features.Orders.GetOrders;

namespace MinimalAPI.Api.Endpoints;

public static class OrderEndpoints
{
    public static WebApplication MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("/", async (
            ISender sender,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? status = null,
            string? date = null) =>
        {
            var result = await sender.Send(new GetOrdersQuery(page, pageSize, search, status, date));
            return TypedResults.Ok(result);
        })
        .WithName("GetOrders")
        .WithSummary("Lấy danh sách đơn hàng có phân trang, lọc và tìm kiếm")
        .Produces<PagedResult<OrderSummaryDto>>();

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderQuery(id));
            return result is not null
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        })
        .WithName("GetOrder")
        .WithSummary("Lấy chi tiết đơn hàng kèm danh sách sản phẩm")
        .Produces<OrderDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async Task<IResult> (CreateOrderCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/orders/{result.Value}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateOrder")
        .WithSummary("Tạo đơn hàng mới (trạng thái Pending)")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/confirm", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ConfirmOrderCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(new { orderId = result.Value, message = "Đã xác nhận đơn hàng và trừ tồn kho thành công." })
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("ConfirmOrder")
        .WithSummary("Xác nhận đơn hàng và tự động trừ tồn kho sản phẩm")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/cancel", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CancelOrderCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(new { orderId = result.Value, message = "Đã hủy đơn hàng." })
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CancelOrder")
        .WithSummary("Hủy đơn hàng (tự động hoàn tồn kho nếu đơn đã từng được xác nhận)")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/complete", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new CompleteOrderCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(new { orderId = result.Value, message = "Đơn hàng đã hoàn tất." })
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CompleteOrder")
        .WithSummary("Đánh dấu đơn hàng hoàn tất")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }
}
