using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Documents.Domain.Aggregates;
using Tms.Documents.Domain.Entities;

namespace Tms.Documents.Infrastructure.Persistence.Configurations;

public sealed class StoredDocumentConfiguration : IEntityTypeConfiguration<StoredDocument>
{
    public void Configure(EntityTypeBuilder<StoredDocument> b)
    {
        b.ToTable("StoredDocuments");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.OwnerType).HasMaxLength(100).IsRequired();
        b.Property(x => x.AccessLevel).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.OwnerId, x.OwnerType });
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.Category);
        b.HasIndex(x => x.ExpiresAt);
    }
}

public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> b)
    {
        b.ToTable("UploadSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.PresignedUploadUrl).HasMaxLength(2000).IsRequired();
        b.Property(x => x.OwnerType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ExpiresAt);
    }
}
