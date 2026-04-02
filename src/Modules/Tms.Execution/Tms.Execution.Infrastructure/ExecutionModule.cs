using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Execution.Application.Features;
using Tms.Execution.Application.Features.UpdateShipmentStatus;
using Tms.Execution.Domain.Interfaces;
using Tms.Execution.Infrastructure.Persistence;
using Tms.Execution.Infrastructure.Persistence.Repositories;

namespace Tms.Execution.Infrastructure;

public static class ExecutionModule
{
    public static IServiceCollection AddExecutionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ExecutionDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "exe"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Repositories
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IPODDocumentRepository, PODDocumentRepository>();
        services.AddScoped<IBlobStorageService, LocalBlobStorageService>();

        // MediatR — Application + Infrastructure handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(PickUpShipmentHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ExecutionModule).Assembly);
        });

        return services;
    }
}
