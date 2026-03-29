using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Execution.Infrastructure.Persistence;

namespace Tms.Execution.Infrastructure;

public static class ExecutionModule
{
    public static IServiceCollection AddExecutionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ExecutionDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("TmsDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "exe")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ExecutionModule).Assembly));

        return services;
    }
}
