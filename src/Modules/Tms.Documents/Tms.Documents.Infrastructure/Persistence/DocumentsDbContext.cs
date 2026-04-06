using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Documents.Domain.Aggregates;
using Tms.Documents.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Documents.Infrastructure.Persistence;

public sealed class DocumentsDbContext(
    DbContextOptions<DocumentsDbContext> options,
    IPublisher publisher) : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();

    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("doc");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "doc");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


