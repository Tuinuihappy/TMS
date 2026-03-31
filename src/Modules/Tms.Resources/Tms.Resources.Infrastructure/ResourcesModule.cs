using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Resources.Application.Features;
using Tms.Resources.Domain.Interfaces;
using Tms.Resources.Infrastructure.Persistence;
using Tms.Resources.Infrastructure.Persistence.Repositories;
using Tms.SharedKernel.Application;

namespace Tms.Resources.Infrastructure;

public static class ResourcesModule
{
    public static IServiceCollection AddResourcesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ResourcesDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "res"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();

        // Cross-module: expose availability checker for Planning module
        services.AddScoped<IResourceAvailabilityChecker, ResourceAvailabilityChecker>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateVehicleHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ResourcesModule).Assembly);
        });

        return services;
    }
}
