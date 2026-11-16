using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilvaStock.Domain.Audit;
using SilvaStock.Domain.Vehicles;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log", table =>
        {
            table.HasCheckConstraint("ck_audit_log_from_status", CheckConstraints.EnumValues<VehicleStatus>("from_status"));
            table.HasCheckConstraint("ck_audit_log_to_status", CheckConstraints.EnumValues<VehicleStatus>("to_status"));
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.At).IsRequired();

        // Admin email as plain text (no FK): history survives deactivated admins.
        builder.Property(e => e.Actor).HasMaxLength(AuditLogEntry.ActorMaxLength).IsRequired();
        builder.Property(e => e.Entity).HasMaxLength(AuditLogEntry.EntityMaxLength).IsRequired();
        builder.Property(e => e.EntityId).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(AuditLogEntry.ActionMaxLength).IsRequired();
        builder.Property(e => e.FromStatus);
        builder.Property(e => e.ToStatus);

        // History of one entity, newest last.
        builder.HasIndex(e => new { e.Entity, e.EntityId, e.At });
    }
}
