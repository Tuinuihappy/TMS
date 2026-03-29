using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Platform.Domain.Entities;

namespace Tms.Platform.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.Property(x => x.PaymentTerms).HasMaxLength(50);
        builder.HasIndex(x => new { x.CustomerCode, x.TenantId }).IsUnique();
    }
}

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.AddressLine).HasMaxLength(500);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.Province).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.Zone).HasMaxLength(50);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.LocationCode, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.Zone);
    }
}

public sealed class ReasonCodeConfiguration : IEntityTypeConfiguration<ReasonCode>
{
    public void Configure(EntityTypeBuilder<ReasonCode> builder)
    {
        builder.ToTable("ReasonCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.Code, x.Category, x.TenantId }).IsUnique();
    }
}

public sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NameTH).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NameEN).HasMaxLength(100);
        builder.Property(x => x.Region).HasMaxLength(50);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => new { x.Date, x.TenantId }).IsUnique();
    }
}

// ── IAM ──────────────────────────────────────────────────────────────────

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.ExternalId).IsUnique();
        builder.Property(x => x.Username).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);

        builder.HasMany<UserRole>("_userRoles")
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_userRoles").HasField("_userRoles").AutoInclude();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.Name, x.TenantId }).IsUnique();

        builder.HasMany<RolePermission>("_permissions")
            .WithOne()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_permissions").HasField("_permissions").AutoInclude();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Resource).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(50);
    }
}

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.KeyHash).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Prefix).IsRequired().HasMaxLength(20);
        builder.Property(x => x.AllowedScopes).HasMaxLength(1000);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Resource).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ResourceId).HasMaxLength(200);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => new { x.TenantId, x.Timestamp });
        builder.Ignore(x => x.DomainEvents);
    }
}
