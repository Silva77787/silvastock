using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.Domain.Vehicles;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", table =>
        {
            table.HasCheckConstraint("ck_vehicles_status", CheckConstraints.EnumValues<VehicleStatus>("status"));
            table.HasCheckConstraint("ck_vehicles_fuel", CheckConstraints.EnumValues<FuelType>("fuel"));
            table.HasCheckConstraint("ck_vehicles_transmission", CheckConstraints.EnumValues<Transmission>("transmission"));
            table.HasCheckConstraint("ck_vehicles_body_type", CheckConstraints.EnumValues<BodyType>("body_type"));
            table.HasCheckConstraint("ck_vehicles_drivetrain", CheckConstraints.EnumValues<Drivetrain>("drivetrain"));
        });

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Status).IsRequired();
        builder.Property(v => v.PublishedAt);
        builder.Property(v => v.SoldAt);

        builder.Property(v => v.Make).HasMaxLength(Vehicle.MakeMaxLength).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(Vehicle.ModelMaxLength).IsRequired();
        builder.Property(v => v.Version).HasMaxLength(Vehicle.VersionMaxLength);
        builder.Property(v => v.Year);
        builder.Property(v => v.MileageKm);
        builder.Property(v => v.Price).HasPrecision(Vehicle.PricePrecision, Vehicle.PriceScale);
        builder.Property(v => v.PriceOnRequest).HasDefaultValue(false).ValueGeneratedNever();
        builder.Property(v => v.Fuel);
        builder.Property(v => v.Transmission);
        builder.Property(v => v.EngineCc);
        builder.Property(v => v.PowerKw);
        builder.Property(v => v.BodyType);
        builder.Property(v => v.Color).HasMaxLength(Vehicle.ColorMaxLength);
        builder.Property(v => v.InteriorColor).HasMaxLength(Vehicle.ColorMaxLength);
        builder.Property(v => v.Doors);
        builder.Property(v => v.Seats);
        builder.Property(v => v.Drivetrain);
        builder.Property(v => v.Emissions).HasMaxLength(Vehicle.EmissionsMaxLength);
        builder.Property(v => v.Description).HasMaxLength(Vehicle.DescriptionMaxLength);

        builder.Property(v => v.Equipment)
            .HasColumnType("text[]")
            .IsRequired()
            .HasDefaultValueSql("'{}'::text[]")
            .ValueGeneratedNever();

        // Private fields: never exposed publicly.
        builder.Property(v => v.RegistrationNumber).HasMaxLength(Vehicle.RegistrationNumberMaxLength);
        builder.Property(v => v.Vin).HasMaxLength(Vehicle.VinLength);
        builder.Property(v => v.InternalNotes).HasMaxLength(Vehicle.InternalNotesMaxLength);

        // Admin emails as plain text (no FK): history survives deactivated admins.
        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.CreatedBy).HasMaxLength(AdminUser.EmailMaxLength).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();
        builder.Property(v => v.UpdatedBy).HasMaxLength(AdminUser.EmailMaxLength).IsRequired();

        builder.HasIndex(v => new { v.Status, v.PublishedAt });
        builder.HasIndex(v => new { v.Make, v.Model });
        builder.HasIndex(v => v.Year);
        builder.HasIndex(v => v.Price);

        builder.HasMany<VehicleImage>()
            .WithOne()
            .HasForeignKey(i => i.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a vehicle is only allowed in Draft (application rule); the cascade keeps rows consistent.
        builder.HasMany<VehicleDocument>()
            .WithOne()
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
