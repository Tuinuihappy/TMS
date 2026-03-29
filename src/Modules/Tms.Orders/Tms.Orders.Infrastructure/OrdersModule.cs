using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseNpgsql(
                configuration.GetConnectionString("TmsDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ord")));

        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // MediatR handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(OrdersModule).Assembly));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(CreateOrderHandler).Assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(CreateOrderValidator).Assembly);

        // Pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
