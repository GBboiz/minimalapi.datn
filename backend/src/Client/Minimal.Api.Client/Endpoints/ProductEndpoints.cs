using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.CreateProduct;
using MinimalAPI.Application.Features.Products.DeleteProduct;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Application.Features.Products.GetProduct;
using MinimalAPI.Application.Features.Products.GetProducts;
using MinimalAPI.Application.Features.Products.SetGiftProduct;
using MinimalAPI.Application.Features.Products.UpdateProduct;

namespace MinimalAPI.Api.Endpoints;

public record SetGiftRequest(Guid? GiftProductId);

/// <summary>Định nghĩa các endpoint REST cho Product (/api/products).</summary>
public static class ProductEndpoints
{
    /// <summary>Đăng ký tất cả endpoint Product vào pipeline.</summary>
    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization();

        group.MapGet("/", async (
            ISender sender,
            int page = 1,
            int pageSize = 10,
            string? search = null) =>
        {
            var result = await sender.Send(new GetProductsQuery(page, pageSize, search));
            return TypedResults.Ok(result);
        })
        .WithName("GetProducts")
        .WithSummary("Lấy danh sách sản phẩm có phân trang")
        .Produces<PagedResult<ProductDto>>();

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductQuery(id));
            return result is not null
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        })
        .WithName("GetProduct")
        .WithSummary("Lấy chi tiết sản phẩm theo Id")
        .Produces<ProductDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async Task<IResult> (CreateProductCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/products/{result.Value}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateProduct")
        .WithSummary("Tạo sản phẩm mới")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateProductCommand command, ISender sender) =>
        {
            var result = await sender.Send(command with { Id = id });
            if (!result.IsSuccess)
            {
                return result.Error == "Sản phẩm không tồn tại."
                    ? TypedResults.NotFound(new { error = result.Error })
                    : TypedResults.BadRequest(new { error = result.Error });
            }

            return TypedResults.Ok(result.Value);
        })
        .WithName("UpdateProduct")
        .WithSummary("Cập nhật sản phẩm")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/gift", async Task<IResult> (Guid id, [FromBody] SetGiftRequest request, ISender sender) =>
        {
            var result = await sender.Send(new SetGiftProductCommand(id, request.GiftProductId));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("SetGiftProduct")
        .WithSummary("Gán hoặc gỡ sản phẩm quà tặng kèm")
        .Produces<Guid>()
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound(new { error = result.Error });
        })
        .WithName("DeleteProduct")
        .WithSummary("Xóa sản phẩm")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
