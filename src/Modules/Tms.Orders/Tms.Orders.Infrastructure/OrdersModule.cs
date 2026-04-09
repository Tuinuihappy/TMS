using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tms.Orders.Application.Events;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Infrastructure.Persistence;
using Tms.Orders.Infrastructure.Persistence.Repositories;
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

        // Cross-module query service — Planning ใช้อ่าน Order data
        services.AddScoped<IOrderQueryService, OrderQueryService>();

        // MediatR handlers — Commands + Event handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(OrdersModule).Assembly));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(CreateOrderHandler).Assembly));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(TripDispatchedOrderHandler).Assembly));

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
