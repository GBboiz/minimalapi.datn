using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Infrastructure.Persistence;
using MinimalAPI.Infrastructure.Persistence.Repositories;

namespace MinimalAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // EF Core — retry on transient failures (network blip, DB restart)
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null)));

        // IApplicationDbContext — query side dùng LINQ (AsNoTracking)
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IGoogleSheetConnectionRepository, GoogleSheetConnectionRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services & Integrations
        services.AddSingleton<IEncryptionService, Services.EncryptionService>();
        services.AddHttpClient<IGoogleSheetsService, Services.GoogleSheetsService>();
        services.AddSingleton<Services.PasswordService>();
        services.AddSingleton<Services.TokenService>();

        // Hosted Background Worker
        services.AddHostedService<BackgroundJobs.GoogleSheetsSyncWorker>();

        return services;
    }
}
