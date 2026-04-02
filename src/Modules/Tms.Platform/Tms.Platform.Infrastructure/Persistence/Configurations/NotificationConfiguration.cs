using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Platform.Domain.Entities;

namespace Tms.Platform.Infrastructure.Persistence.Configurations;

internal sealed class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.ToTable("MessageTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TemplateKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SubjectTemplate).HasMaxLength(200);
        builder.Property(x => x.BodyTemplate).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();

        builder.HasIndex(x => new { x.TemplateKey, x.TenantId }).IsUnique();
    }
}

internal sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("NotificationMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Recipient).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);
    }
}
