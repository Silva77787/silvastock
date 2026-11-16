using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.Domain.Vehicles;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

internal sealed class VehicleDocumentConfiguration : IEntityTypeConfiguration<VehicleDocument>
{
    public void Configure(EntityTypeBuilder<VehicleDocument> builder)
    {
        builder.ToTable("vehicle_documents", table =>
            table.HasCheckConstraint(
                "ck_vehicle_documents_category",
                CheckConstraints.EnumValues<DocumentCategory>("category")));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.VehicleId).IsRequired();
        builder.Property(d => d.Category).IsRequired();
        builder.Property(d => d.DisplayName).HasMaxLength(VehicleDocument.DisplayNameMaxLength).IsRequired();
        builder.Property(d => d.ObjectKey).HasMaxLength(VehicleDocument.ObjectKeyMaxLength).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(VehicleDocument.ContentTypeMaxLength).IsRequired();
        builder.Property(d => d.SizeBytes).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();

        // Admin email as plain text (no FK): history survives deactivated admins.
        builder.Property(d => d.UploadedBy).HasMaxLength(AdminUser.EmailMaxLength).IsRequired();

        builder.HasIndex(d => d.VehicleId);
    }
}
