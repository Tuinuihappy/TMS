# ============================================================
# TMS WebAPI — Multi-stage Dockerfile
# ============================================================

# ── Stage 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first (better layer caching)
COPY Directory.Build.props ./
COPY Tms.slnx ./

# SharedKernel
COPY src/Tms.SharedKernel/Tms.SharedKernel.csproj src/Tms.SharedKernel/

# WebApi
COPY src/Tms.WebApi/Tms.WebApi.csproj src/Tms.WebApi/

# Modules — Orders
COPY src/Modules/Tms.Orders/Tms.Orders.Domain/Tms.Orders.Domain.csproj src/Modules/Tms.Orders/Tms.Orders.Domain/
COPY src/Modules/Tms.Orders/Tms.Orders.Application/Tms.Orders.Application.csproj src/Modules/Tms.Orders/Tms.Orders.Application/
COPY src/Modules/Tms.Orders/Tms.Orders.Infrastructure/Tms.Orders.Infrastructure.csproj src/Modules/Tms.Orders/Tms.Orders.Infrastructure/

# Modules — Planning
COPY src/Modules/Tms.Planning/Tms.Planning.Domain/Tms.Planning.Domain.csproj src/Modules/Tms.Planning/Tms.Planning.Domain/
COPY src/Modules/Tms.Planning/Tms.Planning.Application/Tms.Planning.Application.csproj src/Modules/Tms.Planning/Tms.Planning.Application/
COPY src/Modules/Tms.Planning/Tms.Planning.Infrastructure/Tms.Planning.Infrastructure.csproj src/Modules/Tms.Planning/Tms.Planning.Infrastructure/

# Modules — Execution
COPY src/Modules/Tms.Execution/Tms.Execution.Domain/Tms.Execution.Domain.csproj src/Modules/Tms.Execution/Tms.Execution.Domain/
COPY src/Modules/Tms.Execution/Tms.Execution.Application/Tms.Execution.Application.csproj src/Modules/Tms.Execution/Tms.Execution.Application/
COPY src/Modules/Tms.Execution/Tms.Execution.Infrastructure/Tms.Execution.Infrastructure.csproj src/Modules/Tms.Execution/Tms.Execution.Infrastructure/

# Modules — Resources
COPY src/Modules/Tms.Resources/Tms.Resources.Domain/Tms.Resources.Domain.csproj src/Modules/Tms.Resources/Tms.Resources.Domain/
COPY src/Modules/Tms.Resources/Tms.Resources.Application/Tms.Resources.Application.csproj src/Modules/Tms.Resources/Tms.Resources.Application/
COPY src/Modules/Tms.Resources/Tms.Resources.Infrastructure/Tms.Resources.Infrastructure.csproj src/Modules/Tms.Resources/Tms.Resources.Infrastructure/

# Modules — Platform
COPY src/Modules/Tms.Platform/Tms.Platform.Domain/Tms.Platform.Domain.csproj src/Modules/Tms.Platform/Tms.Platform.Domain/
COPY src/Modules/Tms.Platform/Tms.Platform.Application/Tms.Platform.Application.csproj src/Modules/Tms.Platform/Tms.Platform.Application/
COPY src/Modules/Tms.Platform/Tms.Platform.Infrastructure/Tms.Platform.Infrastructure.csproj src/Modules/Tms.Platform/Tms.Platform.Infrastructure/

# Modules — Tracking
COPY src/Modules/Tms.Tracking/Tms.Tracking.Domain/Tms.Tracking.Domain.csproj src/Modules/Tms.Tracking/Tms.Tracking.Domain/
COPY src/Modules/Tms.Tracking/Tms.Tracking.Application/Tms.Tracking.Application.csproj src/Modules/Tms.Tracking/Tms.Tracking.Application/
COPY src/Modules/Tms.Tracking/Tms.Tracking.Infrastructure/Tms.Tracking.Infrastructure.csproj src/Modules/Tms.Tracking/Tms.Tracking.Infrastructure/

# Restore dependencies (cached layer)
RUN dotnet restore src/Tms.WebApi/Tms.WebApi.csproj

# Copy all source code
COPY src/ src/

# Build & Publish
RUN dotnet publish src/Tms.WebApi/Tms.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd --system --gid 1001 appgroup && \
    useradd --system --uid 1001 --gid appgroup --no-create-home appuser

COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "Tms.WebApi.dll"]
