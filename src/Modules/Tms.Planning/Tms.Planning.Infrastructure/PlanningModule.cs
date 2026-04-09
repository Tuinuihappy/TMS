using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Planning.Application;
using Tms.Planning.Application.Features;
using Tms.Planning.Domain.Interfaces;
using Tms.Planning.Infrastructure.Persistence;
using Tms.Planning.Infrastructure.Persistence.Repositories;
using Tms.SharedKernel.Application;

namespace Tms.Planning.Infrastructure;

public static class PlanningModule
{
    public static IServiceCollection AddPlanningModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PlanningDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pln"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IRoutePlanRepository, RoutePlanRepository>();
        services.AddScoped<IOptimizationRequestRepository, OptimizationRequestRepository>();
        services.AddScoped<GreedyRouteOptimizer>();
        services.AddScoped<PdpRouteOptimizer>();

        // IOutboxWriter — backed by PlanningDbContext (transactional outbox per module)
        services.AddScoped<IOutboxWriter>(sp =>
            new OutboxWriter<PlanningDbContext>(sp.GetRequiredService<PlanningDbContext>()));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateTripHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(PlanningModule).Assembly);
        });

        return services;
    }
}
