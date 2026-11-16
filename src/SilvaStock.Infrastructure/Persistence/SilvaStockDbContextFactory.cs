using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SilvaStock.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> (migrations, bundles), so the tools never start the API.
/// Reads the environment variable <c>ConnectionStrings__Default</c>; otherwise falls back to a local
/// database without credentials, which is enough to add or script migrations (no connection is opened).
/// </summary>
public sealed class SilvaStockDbContextFactory : IDesignTimeDbContextFactory<SilvaStockDbContext>
{
    /// <summary>Environment variable holding the connection string (same name as the API configuration).</summary>
    public const string ConnectionStringVariable = "ConnectionStrings__Default";

    /// <summary>Local fallback; no password (never put secrets here).</summary>
    public const string FallbackConnectionString = "Host=localhost;Port=5432;Database=silvastock;Username=silvastock";

    public SilvaStockDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = FallbackConnectionString;
        }

        var builder = new DbContextOptionsBuilder<SilvaStockDbContext>();
        SilvaStockDbContext.Configure(builder, connectionString);
        return new SilvaStockDbContext(builder.Options);
    }
}
