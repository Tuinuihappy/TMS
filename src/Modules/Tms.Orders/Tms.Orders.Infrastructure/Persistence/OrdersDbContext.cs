using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Orders.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
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

        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "ord");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


