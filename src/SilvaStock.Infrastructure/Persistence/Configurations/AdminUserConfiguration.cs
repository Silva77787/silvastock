using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

internal sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users", table =>
            // Uniqueness is case-insensitive because only the lower-case form can be stored.
            table.HasCheckConstraint("ck_admin_users_email_lower", "email = lower(email)"));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(AdminUser.EmailMaxLength).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(AdminUser.DisplayNameMaxLength);

        // Store defaults for rows created outside EF (e.g. SQL); EF always sends the entity's value.
        builder.Property(u => u.IsActive).HasDefaultValue(true).ValueGeneratedNever();
        builder.Property(u => u.SessionVersion).HasDefaultValue(0).ValueGeneratedNever();

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.GoogleSub);
        builder.HasIndex(u => u.GoogleSub).IsUnique();
    }
}
