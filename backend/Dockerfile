# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY *.slnx .

# Admin Projects
COPY src/Admin/Minimal.Domain.Admin/*.csproj src/Admin/Minimal.Domain.Admin/
COPY src/Admin/Minimal.Application.Admin/*.csproj src/Admin/Minimal.Application.Admin/
COPY src/Admin/Minimal.Infrastructure.Admin/*.csproj src/Admin/Minimal.Infrastructure.Admin/
COPY src/Admin/MinimalAPI.Api.Admin/*.csproj src/Admin/MinimalAPI.Api.Admin/

# Client Projects
COPY src/Client/Minimal.Domain.Client/*.csproj src/Client/Minimal.Domain.Client/
COPY src/Client/Minimal.Application.Client/*.csproj src/Client/Minimal.Application.Client/
COPY src/Client/Minimal.Infrastructure.Client/*.csproj src/Client/Minimal.Infrastructure.Client/
COPY src/Client/Minimal.Api.Client/*.csproj src/Client/Minimal.Api.Client/

RUN dotnet restore
COPY . .
RUN dotnet publish src/Client/Minimal.Api.Client -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "Minimal.Api.Client.dll"]
