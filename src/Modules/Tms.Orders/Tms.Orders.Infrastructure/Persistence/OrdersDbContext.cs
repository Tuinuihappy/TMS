using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Orders.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<TransportOrder> TransportOrders => Set<TransportOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ord");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Tms.SharedKernel.Domain.AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var versionProp = entityType.FindProperty("Version");
            if (versionProp is not null)
                versionProp.IsConcurrencyToken = false;
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.DispatchDomainEventsAsync(this, publisher, cancellationToken);
        return result;
    }
}
