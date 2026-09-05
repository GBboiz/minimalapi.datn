using System.Threading.RateLimiting;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Api.Endpoints;
using MinimalAPI.Application;
using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Infrastructure;
using MinimalAPI.Infrastructure.Persistence;
using Serilog;
using Microsoft.IdentityModel.Tokens;
using MinimalAPI.Infrastructure.Services;
using MinimalAPI.Api.Services;
using MinimalAPI.Api.Admin.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Serilog — đọc config từ appsettings
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MinimalAPI.Application.Abstractions.ICurrentStore, CurrentStore>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// Health checks — DB connectivity
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// Rate limiting — chống spam/DDoS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ProblemDetails + Swagger
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MinimalAPI - Quản lý Bán hàng & Tồn kho",
        Version = "v1",
        Description = "Hệ thống quản lý bán hàng đa cửa hàng (Multi-tenant), trừ tồn kho tự động và đồng bộ Google Sheets qua Outbox Pattern."
    });

    var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Nhập access token nhận được từ /api/auth/login. Ví dụ: Bearer {token}"
    };

    c.AddSecurityDefinition("Bearer", scheme);

    c.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration.GetValue<string>("AllowedOrigins") ?? "";
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Global exception handler — trả ProblemDetails chuẩn RFC 9457
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var problemDetails = exception switch
        {
            ValidationException validationEx => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = 400,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier,
                    ["errors"] = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray())
                }
            },
            DomainException domainEx => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = 400,
                Title = "Domain Error",
                Detail = domainEx.Message,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            },
            _ => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = 500,
                Title = "Internal Server Error",
                Detail = "Đã xảy ra lỗi hệ thống.",
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// HTTPS redirect — production chạy sau reverse proxy (nginx/traefik) nên bỏ qua
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Swagger — chỉ Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map endpoints
app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapCustomerEndpoints();
app.MapOrderEndpoints();
app.MapAuthEndpoints();
app.MapGoogleSheetEndpoints();
app.MapDashboardEndpoints();
app.MapAdminStoreEndpoints();
app.MapAiScannerEndpoints();
app.MapHealthChecks("/health");

// Auto migrate — idempotent, an toàn cho mọi environment
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed data — chỉ Development
    if (app.Environment.IsDevelopment())
    {
        await SeedData.SeedAsync(db);
    }
}

app.Run();
