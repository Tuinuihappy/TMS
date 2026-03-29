using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Planning.Infrastructure.Persistence;

namespace Tms.Planning.Infrastructure;

public static class PlanningModule
{
    public static IServiceCollection AddPlanningModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PlanningDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("TmsDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pln")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(PlanningModule).Assembly));

        return services;
    }
}
