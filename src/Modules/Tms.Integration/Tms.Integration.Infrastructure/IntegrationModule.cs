using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Integration.Application.Acl;
using Tms.Integration.Application.Abstractions;
using Tms.Integration.Application.Features.OmsIntegration.ProcessSync;
using Tms.Integration.Application.Features.OmsIntegration.PushStatus;
using Tms.Integration.Application.Features.AmrIntegration.ReceiveAmrEvent;
using Tms.Integration.Domain.Interfaces;
using Tms.Integration.Infrastructure.Http;
using Tms.Integration.Infrastructure.Persistence;
using Tms.Integration.Infrastructure.Persistence.Repositories;

namespace Tms.Integration.Infrastructure;

public static class IntegrationModule
{
    public static IServiceCollection AddIntegrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<IntegrationDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "itg"))
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Repositories
        services.AddScoped<IOmsSyncRepository, OmsSyncRepository>();
        services.AddScoped<IAmrHandoffRepository, AmrHandoffRepository>();
        services.AddScoped<IErpExportRepository, ErpExportRepository>();

        // ACL + Abstractions
        services.AddScoped<OmsAclMapper>();
        services.AddScoped<IErpHttpClient, StubErpHttpClient>();
        services.AddScoped<IOmsCallbackSender, OmsCallbackSender>();

        // HttpClient สำหรับ OMS Outbox (ชื่อ "DEFAULT_OMS" ← configure base address ใน appsettings)
        services.AddHttpClient("DEFAULT_OMS", client =>
        {
            var baseUrl = configuration["Integration:OmsCallbackUrl"] ?? "http://localhost:9999";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler();

        // MediatR — Application + Infrastructure handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ReceiveAmrEventHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(IntegrationModule).Assembly);
        });

        // Background Workers
        services.AddHostedService<ProcessOmsSyncWorker>();
        services.AddHostedService<OmsOutboxWorker>();

        return services;
    }
}
