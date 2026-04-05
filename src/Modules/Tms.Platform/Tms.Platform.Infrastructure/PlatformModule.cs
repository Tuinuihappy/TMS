using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Platform.Application.Features.Iam;
using Tms.Platform.Application.Features.MasterData;
using Tms.Platform.Application.Features.Notifications;
using Tms.Platform.Domain.Interfaces;
using Tms.Platform.Infrastructure.Persistence;
using Tms.Platform.Infrastructure.Persistence.Repositories;

namespace Tms.Platform.Infrastructure;

public static class PlatformModule
{
    public static IServiceCollection AddPlatformModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PlatformDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plf"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Master Data Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IReasonCodeRepository, ReasonCodeRepository>();
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();

        // IAM Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Notification Repositories + Sender (Phase 2)
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationSender, StubNotificationSender>();

        // Audit Log Writer (bridges SharedKernel → Platform)
        services.AddScoped<Tms.SharedKernel.Application.IAuditLogWriter, AuditLogWriter>();

        // MediatR — Application + Infrastructure handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateCustomerHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(SyncUserHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(SendNotificationHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(PlatformModule).Assembly);
        });

        return services;
    }
}
