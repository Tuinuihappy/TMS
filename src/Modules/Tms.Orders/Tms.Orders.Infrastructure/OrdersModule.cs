using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Tms.Orders.Application.Abstractions;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.Orders.Application.Features.GetOrders;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Infrastructure.Cache;
using Tms.Orders.Infrastructure.Persistence;
using Tms.Orders.Infrastructure.Persistence.Repositories;
using Tms.Orders.Infrastructure.ReadServices;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Infrastructure;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<OrdersDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("TmsDb"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ord"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Read services (CQRS read side — LINQ projection, no entity tracking)
        services.AddScoped<IOrderReadService, OrderReadService>();

        // Cache invalidator — called by handlers that mutate Orders
        services.AddScoped<IOrderCacheInvalidator, OrderCacheInvalidator>();

        // Cross-module query service — Planning ใช้อ่าน Order data
        // CachedOrderQueryService decorates OrderQueryService with Redis cache (5-min TTL)
        services.AddScoped<OrderQueryService>();
        services.AddScoped<IOrderQueryService>(sp =>
            new CachedOrderQueryService(
                sp.GetRequiredService<OrderQueryService>(),
                sp.GetRequiredService<IDistributedCache>()));

        // MediatR handlers — Infrastructure + Application assemblies
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(
                typeof(OrdersModule).Assembly,
                typeof(CreateOrderHandler).Assembly));

        // IOutboxWriter — backed by OrdersDbContext
        services.AddScoped<IOutboxWriter>(sp =>
            new OutboxWriter<OrdersDbContext>(sp.GetRequiredService<OrdersDbContext>()));


        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(CreateOrderValidator).Assembly);

        // Pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
