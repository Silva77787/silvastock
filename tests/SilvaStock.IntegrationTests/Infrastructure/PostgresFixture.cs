using Testcontainers.PostgreSql;

namespace SilvaStock.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one disposable PostgreSQL container per test run (shared by every class in
/// <see cref="IntegrationTestGroup"/>) and creates API factories bound to it.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>Pinned image; keep in line with the Neon major version (ADR-0003).</summary>
    public const string PostgresImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase("silvastock_tests")
        .WithUsername("silvastock")
        .WithPassword("silvastock-test-only")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates a factory for the API connected to the test container.</summary>
    public SilvaStockApiFactory CreateFactory(string environment = SilvaStockApiFactory.TestingEnvironment)
        => new(ConnectionString, environment);

    /// <summary>
    /// Empties every table in the <c>public</c> schema (except the EF migrations history) so each
    /// test starts from a clean database. Runs through <c>psql</c> inside the container, so the
    /// test project needs no direct database driver.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        const string sql = """
            DO $$
            DECLARE tables text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                  INTO tables
                  FROM pg_tables
                 WHERE schemaname = 'public'
                   AND tablename <> '__EFMigrationsHistory';
                IF tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || tables || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """;

        var result = await _container.ExecScriptAsync(sql);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Database reset failed: {result.Stderr}");
        }
    }
}

/// <summary>Collection that shares a single <see cref="PostgresFixture"/> across test classes.</summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Integration (Postgres)";
}
