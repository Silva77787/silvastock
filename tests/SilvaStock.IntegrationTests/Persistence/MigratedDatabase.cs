using Microsoft.EntityFrameworkCore;
using SilvaStock.Infrastructure.Persistence;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Persistence;

/// <summary>Contexts bound to the test container, with the schema created by the real migrations (never EnsureCreated).</summary>
internal static class MigratedDatabase
{
    public static SilvaStockDbContext CreateContext(PostgresFixture postgres)
    {
        var builder = new DbContextOptionsBuilder<SilvaStockDbContext>();
        SilvaStockDbContext.Configure(builder, postgres.ConnectionString);
        return new SilvaStockDbContext(builder.Options);
    }

    /// <summary>Applies pending migrations and empties every table so the test starts from a clean database.</summary>
    public static async Task PrepareAsync(PostgresFixture postgres)
    {
        await using (var context = CreateContext(postgres))
        {
            await context.Database.MigrateAsync();
        }

        await postgres.ResetDatabaseAsync();
    }
}
