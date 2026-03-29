using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Platform.Infrastructure.Persistence;

namespace Tms.Platform.Infrastructure;

public static class PlatformModule
{
    public static IServiceCollection AddPlatformModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("TmsDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plf")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(PlatformModule).Assembly));

        return services;
    }
}
