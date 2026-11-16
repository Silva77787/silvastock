using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilvaStock.Domain.Vehicles;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

internal sealed class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImage>
{
    public void Configure(EntityTypeBuilder<VehicleImage> builder)
    {
        builder.ToTable("vehicle_images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.VehicleId).IsRequired();
        builder.Property(i => i.ObjectKey).HasMaxLength(VehicleImage.ObjectKeyMaxLength).IsRequired();
        builder.Property(i => i.ThumbKey).HasMaxLength(VehicleImage.ObjectKeyMaxLength).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(VehicleImage.ContentTypeMaxLength).IsRequired();
        builder.Property(i => i.SizeBytes).IsRequired();
        builder.Property(i => i.SortOrder).IsRequired();
        builder.Property(i => i.IsPrimary).IsRequired();

        // Gallery order; also serves the foreign key lookups on vehicle_id.
        builder.HasIndex(i => new { i.VehicleId, i.SortOrder });

        // At most one primary image per vehicle.
        builder.HasIndex(i => i.VehicleId, "PrimaryImagePerVehicle")
            .HasDatabaseName("ix_vehicle_images_vehicle_id_primary")
            .IsUnique()
            .HasFilter("is_primary");
    }
}
