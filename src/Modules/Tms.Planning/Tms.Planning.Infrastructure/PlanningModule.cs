using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Planning.Application.Features;
using Tms.Planning.Domain.Interfaces;
using Tms.Planning.Infrastructure.Persistence;
using Tms.Planning.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<ITripRepository, TripRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateTripHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(PlanningModule).Assembly);
        });

        return services;
    }
}
