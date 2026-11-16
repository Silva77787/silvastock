using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

/// <summary>
/// Framework-owned entity of the Data Protection key ring. Keeps the package's <c>int</c> key, an accepted
/// exception to the bigint rule (ADR-0004). Persisting keys is wired in F2-04; this only defines the table.
/// </summary>
internal sealed class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder) => builder.ToTable("data_protection_keys");
}
