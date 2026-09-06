using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Promotions.CreatePromotion;
using MinimalAPI.Application.Features.Promotions.DeletePromotion;
using MinimalAPI.Application.Features.Promotions.DTOs;
using MinimalAPI.Application.Features.Promotions.GetPromotions;
using MinimalAPI.Application.Features.Promotions.UpdatePromotion;

namespace MinimalAPI.Api.Endpoints;

public static class PromotionEndpoints
{
    public static WebApplication MapPromotionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/promotions")
            .WithTags("Promotions")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, bool? onlyActive = null) =>
        {
            var result = await sender.Send(new GetPromotionsQuery(onlyActive));
            return TypedResults.Ok(result);
        })
        .WithName("GetPromotions")
        .WithSummary("Lấy danh sách chương trình khuyến mãi")
        .Produces<List<PromotionDto>>();

        group.MapPost("/", async Task<IResult> (CreatePromotionCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/promotions/{result.Value}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreatePromotion")
        .WithSummary("Tạo chương trình khuyến mãi mới")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdatePromotionCommand command, ISender sender) =>
        {
            var result = await sender.Send(command with { Id = id });
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("UpdatePromotion")
        .WithSummary("Cập nhật chương trình khuyến mãi")
        .Produces<Guid>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeletePromotionCommand(id));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("DeletePromotion")
        .WithSummary("Xóa chương trình khuyến mãi")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }
}
