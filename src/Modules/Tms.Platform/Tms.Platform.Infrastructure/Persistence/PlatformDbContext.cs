using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Platform.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options, IPublisher publisher) : DbContext(options)
{
    // Master Data
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ReasonCode> ReasonCodes => Set<ReasonCode>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    // IAM
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Notification (Phase 2)
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("plf");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);

        // Disable Version as concurrency token (EF auto-detects by convention)
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
