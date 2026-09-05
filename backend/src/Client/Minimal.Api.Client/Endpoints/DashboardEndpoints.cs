using MediatR;
using MinimalAPI.Application.Features.Dashboard.GetStats;

namespace MinimalAPI.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/stats", async (ISender sender) =>
        {
            var result = await sender.Send(new GetDashboardStatsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetDashboardStats")
        .WithSummary("Lấy số liệu thống kê tổng quan (Doanh thu, Đơn hàng, Tồn kho, Khách hàng) cho cửa hàng.");

        return group;
    }
}
