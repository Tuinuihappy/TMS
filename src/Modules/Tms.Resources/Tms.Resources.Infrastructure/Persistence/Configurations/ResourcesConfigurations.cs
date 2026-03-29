using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Resources.Domain.Entities;

namespace Tms.Resources.Infrastructure.Persistence.Configurations;

public sealed class VehicleTypeConfiguration : IEntityTypeConfiguration<VehicleType>
{
    public void Configure(EntityTypeBuilder<VehicleType> builder)
    {
        builder.ToTable("VehicleTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MaxPayloadKg).HasPrecision(10, 2);
        builder.Property(x => x.MaxVolumeCBM).HasPrecision(10, 2);
        builder.Property(x => x.RequiredLicenseType).HasMaxLength(10);
    }
}

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlateNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.PlateNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.VehicleTypeId);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Ownership).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.SubcontractorName).HasMaxLength(200);
        builder.Property(x => x.CurrentOdometerKm).HasPrecision(12, 2);

        builder.HasMany<MaintenanceRecord>(v => v.MaintenanceRecords)
            .WithOne().HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.MaintenanceRecords)
            .HasField("_maintenanceRecords")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude(false);

        builder.HasMany<InsuranceRecord>(v => v.InsuranceRecords)
            .WithOne().HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.InsuranceRecords)
            .HasField("_insuranceRecords")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude(false);
    }
}

public sealed class MaintenanceConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.VehicleId);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.OdometerAtService).HasPrecision(12, 2);
        builder.Property(x => x.Cost).HasPrecision(10, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class InsuranceConfiguration : IEntityTypeConfiguration<InsuranceRecord>
{
    public void Configure(EntityTypeBuilder<InsuranceRecord> builder)
    {
        builder.ToTable("InsuranceRecords");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.ExpiryDate);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PolicyNumber).HasMaxLength(100);
        builder.Property(x => x.Provider).HasMaxLength(200);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.EmployeeCode).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.SuspendReason).HasMaxLength(500);
        builder.Property(x => x.PerformanceScore).HasPrecision(3, 1);

        builder.OwnsOne(x => x.License, l =>
        {
            l.Property(p => p.LicenseNumber).HasColumnName("License_Number").HasMaxLength(20);
            l.Property(p => p.LicenseType).HasColumnName("License_Type").IsRequired().HasMaxLength(10);
            l.Property(p => p.ExpiryDate).HasColumnName("License_ExpiryDate");
        });

        builder.HasMany<HOSRecord>(d => d.HOSHistory)
            .WithOne().HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(d => d.HOSHistory)
            .HasField("_hosHistory")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude(false);
    }
}

public sealed class HOSRecordConfiguration : IEntityTypeConfiguration<HOSRecord>
{
    public void Configure(EntityTypeBuilder<HOSRecord> builder)
    {
        builder.ToTable("HOSRecords");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DriverId);
        builder.HasIndex(x => x.Date);
        builder.Property(x => x.DrivingHours).HasPrecision(4, 1);
        builder.Property(x => x.RestingHours).HasPrecision(4, 1);
        builder.Ignore(x => x.DomainEvents);
    }
}
