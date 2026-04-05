using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Tracking.Application.Features;
using Tms.Tracking.Domain.Interfaces;
using Tms.Tracking.Infrastructure.Persistence;
using Tms.Tracking.Infrastructure.Persistence.Repositories;

namespace Tms.Tracking.Infrastructure;

public static class TrackingModule
{
    public static IServiceCollection AddTrackingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TrackingDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "trk"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Repositories
        services.AddScoped<IVehiclePositionRepository, VehiclePositionRepository>();
        services.AddScoped<ICurrentVehicleStateRepository, CurrentVehicleStateRepository>();
        services.AddScoped<IGeoZoneRepository, GeoZoneRepository>();
        services.AddScoped<IZoneEventRepository, ZoneEventRepository>();

        // MediatR handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IngestPositionsHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(TrackingModule).Assembly);
        });

        // Background worker for geofence checks
        services.AddHostedService<GeofenceBackgroundWorker>();

        // Background worker: Data retention — clean old vehicle positions
        services.AddHostedService<DataRetentionBackgroundWorker>();

        return services;
    }
}
