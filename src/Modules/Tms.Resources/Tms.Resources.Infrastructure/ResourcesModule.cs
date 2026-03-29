using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Resources.Infrastructure.Persistence;

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

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ResourcesModule).Assembly));

        return services;
    }
}
