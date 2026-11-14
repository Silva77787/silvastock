namespace SilvaStock.IntegrationTests.Infrastructure;

/// <summary>Self-checks for the shared test infrastructure (independent of product endpoints).</summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class PostgresFixtureTests(PostgresFixture postgres)
{
    [Fact]
    public void ContainerExposesConnectionStringForTheTestDatabase()
    {
        Assert.Contains("Database=silvastock_tests", postgres.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetDatabaseSucceedsOnTheRunningContainer()
    {
        // Must not throw, with or without tables in the public schema.
        await postgres.ResetDatabaseAsync();
        await postgres.ResetDatabaseAsync();
    }
}
