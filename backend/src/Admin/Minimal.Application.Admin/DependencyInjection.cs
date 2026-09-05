using Microsoft.Extensions.DependencyInjection;

namespace Minimal.Application.Admin;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        return services;
    }
}
