using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Resources.Application.Features;
using Tms.Resources.Domain.Interfaces;
using Tms.Resources.Infrastructure.Persistence;
using Tms.Resources.Infrastructure.Persistence.Repositories;

namespace Tms.Resources.Infrastructure;

public static class ResourcesModule
{
    public static IServiceCollection AddResourcesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ResourcesDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("TmsDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "res")));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateVehicleHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ResourcesModule).Assembly);
        });

        return services;
    }
}
