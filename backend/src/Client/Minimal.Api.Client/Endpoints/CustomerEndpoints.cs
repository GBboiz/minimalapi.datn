using MediatR;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.CreateCustomer;
using MinimalAPI.Application.Features.Customers.DeleteCustomer;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Application.Features.Customers.GetCustomer;
using MinimalAPI.Application.Features.Customers.GetCustomers;
using MinimalAPI.Application.Features.Customers.UpdateCustomer;

namespace MinimalAPI.Api.Endpoints;

public static class CustomerEndpoints
{
    public static WebApplication MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, int page = 1, int pageSize = 20, string? search = null) =>
        {
            var result = await sender.Send(new GetCustomersQuery(page, pageSize, search));
            return TypedResults.Ok(result);
        })
        .WithName("GetCustomers")
        .WithSummary("Lấy danh sách khách hàng có phân trang và tìm kiếm")
        .Produces<PagedResult<CustomerDto>>();

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCustomerQuery(id));
            return result is not null
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        })
        .WithName("GetCustomer")
        .WithSummary("Lấy chi tiết khách hàng theo Id")
        .Produces<CustomerDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async Task<IResult> (CreateCustomerCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/customers/{result.Value}", result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("CreateCustomer")
        .WithSummary("Tạo khách hàng mới")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateCustomerCommand command, ISender sender) =>
        {
            var result = await sender.Send(command with { Id = id });
            if (!result.IsSuccess)
            {
                return result.Error == "Khách hàng không tồn tại."
                    ? TypedResults.NotFound(new { error = result.Error })
                    : TypedResults.BadRequest(new { error = result.Error });
            }

            return TypedResults.Ok(result.Value);
        })
        .WithName("UpdateCustomer")
        .WithSummary("Cập nhật thông tin khách hàng")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCustomerCommand(id));
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.BadRequest(new { error = result.Error });
        })
        .WithName("DeleteCustomer")
        .WithSummary("Xóa khách hàng")
        .Produces<Guid>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }
}
