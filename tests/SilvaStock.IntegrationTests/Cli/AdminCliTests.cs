using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SilvaStock.Cli;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.IntegrationTests.Infrastructure;
using SilvaStock.IntegrationTests.Persistence;

namespace SilvaStock.IntegrationTests.Cli;

/// <summary>The allowlist CLI (F2-05) run in-process against the migrated test database.</summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class AdminCliTests(PostgresFixture postgres) : IAsyncLifetime, IDisposable
{
    private const string _maria = "maria@example.test";

    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public Task InitializeAsync() => MigratedDatabase.PrepareAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
    }

    [Fact]
    public async Task AddAdminCreatesAnActiveLowerCaseAdmin()
    {
        var exitCode = await RunAsync("add-admin", "  Maria@Example.TEST ", "--name", "Maria Silva");

        Assert.Equal(0, exitCode);
        var admin = await SingleAdminAsync();
        Assert.Equal(_maria, admin.Email);
        Assert.Equal("Maria Silva", admin.DisplayName);
        Assert.True(admin.IsActive);
        Assert.Equal(0, admin.SessionVersion);
        Assert.Null(admin.GoogleSub);
        Assert.Null(admin.LastLoginAt);
        Assert.Contains("Created administrator maria@example.test", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddAdminIsIdempotentAndCaseInsensitive()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria, "--name", "Maria"));
        var before = await SingleAdminAsync();

        Assert.Equal(0, await RunAsync("add-admin", "MARIA@example.TEST"));
        Assert.Equal(0, await RunAsync("add-admin", _maria, "--name", "Maria"));

        var after = await SingleAdminAsync();
        Assert.Equal(before.Id, after.Id);
        Assert.Equal("Maria", after.DisplayName);
        Assert.True(after.IsActive);
        Assert.Equal(0, after.SessionVersion);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Contains("already active", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddAdminWithANewNameUpdatesTheDisplayName()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria, "--name", "Maria"));

        Assert.Equal(0, await RunAsync("add-admin", _maria, "--name", "Maria Silva"));

        Assert.Equal("Maria Silva", (await SingleAdminAsync()).DisplayName);
    }

    [Fact]
    public async Task RemoveAdminDeactivatesKeepsTheRowAndIncrementsTheSessionVersion()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));

        var exitCode = await RunAsync("remove-admin", "Maria@EXAMPLE.test");

        Assert.Equal(0, exitCode);
        var admin = await SingleAdminAsync();
        Assert.False(admin.IsActive);
        Assert.Equal(1, admin.SessionVersion);
    }

    [Fact]
    public async Task RemoveAdminTwiceStaysInactiveAndRevokesAgain()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));
        Assert.Equal(0, await RunAsync("remove-admin", _maria));

        Assert.Equal(0, await RunAsync("remove-admin", _maria));

        var admin = await SingleAdminAsync();
        Assert.False(admin.IsActive);
        Assert.Equal(2, admin.SessionVersion);
    }

    [Fact]
    public async Task AddAdminReactivatesARemovedAdminWithoutResettingTheSessionVersion()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria, "--name", "Maria"));
        Assert.Equal(0, await RunAsync("remove-admin", _maria));

        var exitCode = await RunAsync("add-admin", "MARIA@example.test");

        Assert.Equal(0, exitCode);
        var admin = await SingleAdminAsync();
        Assert.True(admin.IsActive);
        Assert.Equal(1, admin.SessionVersion);
        Assert.Equal("Maria", admin.DisplayName);
        Assert.Contains("Reactivated administrator maria@example.test", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetGoogleClearsTheSubAndIncrementsTheSessionVersion()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));
        await BindGoogleAccountAsync(_maria, "google-sub-example-1");

        var exitCode = await RunAsync("reset-google", "MARIA@example.test");

        Assert.Equal(0, exitCode);
        var admin = await SingleAdminAsync();
        Assert.Null(admin.GoogleSub);
        Assert.Equal(1, admin.SessionVersion);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task ResetGoogleWithoutABoundAccountStillIncrementsTheSessionVersion()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));

        Assert.Equal(0, await RunAsync("reset-google", _maria));

        Assert.Equal(1, (await SingleAdminAsync()).SessionVersion);
    }

    [Theory]
    [InlineData("remove-admin")]
    [InlineData("reset-google")]
    public async Task UnknownEmailReturnsTwoAndChangesNothing(string command)
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));

        var exitCode = await RunAsync(command, "nobody@example.test");

        Assert.Equal(2, exitCode);
        Assert.Contains("no administrator with email nobody@example.test", _error.ToString(), StringComparison.Ordinal);
        var admin = await SingleAdminAsync();
        Assert.True(admin.IsActive);
        Assert.Equal(0, admin.SessionVersion);
    }

    [Fact]
    public async Task InvalidUsageReturnsOneAndChangesNothing()
    {
        Assert.Equal(1, await RunAsync());
        Assert.Equal(1, await RunAsync("add-admin", "not-an-email"));
        Assert.Equal(1, await RunAsync("add-admin", _maria, "--name", new string('n', 101)));

        await using var context = MigratedDatabase.CreateContext(postgres);
        Assert.Empty(await context.AdminUsers.ToListAsync());
    }

    [Fact]
    public async Task ListAdminsShowsEveryAdminWithStatusLastLoginAndGoogleBinding()
    {
        Assert.Equal(0, await RunAsync("add-admin", "bea@example.test", "--name", "Bea Costa"));
        Assert.Equal(0, await RunAsync("add-admin", _maria));
        Assert.Equal(0, await RunAsync("remove-admin", _maria));
        await BindGoogleAccountAsync("bea@example.test", "google-sub-example-2");
        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            await context.Database.ExecuteSqlAsync(
                $"UPDATE admin_users SET last_login_at = '2026-11-15T09:30:00Z' WHERE email = 'bea@example.test'");
        }

        _output.GetStringBuilder().Clear();
        var exitCode = await RunAsync("list-admins");

        Assert.Equal(0, exitCode);
        var lines = _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Matches(@"^EMAIL\s+NAME\s+ACTIVE\s+LAST LOGIN \(UTC\)\s+GOOGLE BOUND$", lines[0]);
        Assert.Matches(@"^bea@example\.test\s+Bea Costa\s+yes\s+2026-11-15 09:30:00\s+yes$", lines[1]);
        Assert.Matches(@"^maria@example\.test\s+-\s+no\s+never\s+no$", lines[2]);
        Assert.DoesNotContain("google-sub-example-2", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListAdminsWithNoAdmins()
    {
        Assert.Equal(0, await RunAsync("list-admins"));

        Assert.Equal("No administrators.", _output.ToString().Trim());
    }

    [Fact]
    public async Task OutputNeverContainsTheConnectionString()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));
        Assert.Equal(0, await RunAsync("list-admins"));
        Assert.Equal(2, await RunAsync("remove-admin", "nobody@example.test"));

        var all = _output.ToString() + _error.ToString();
        Assert.DoesNotContain("silvastock-test-only", all, StringComparison.Ordinal);
        Assert.DoesNotContain(postgres.ConnectionString, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConcurrentRevocationsAreNeverLost()
    {
        Assert.Equal(0, await RunAsync("add-admin", _maria));

        // Each run reads and increments session_version; the row lock serialises them.
        var runs = Enumerable.Range(0, 8)
            .Select(i => RunWithOwnWritersAsync(i % 2 == 0 ? "reset-google" : "remove-admin", _maria));
        var exitCodes = await Task.WhenAll(runs);

        Assert.All(exitCodes, code => Assert.Equal(0, code));
        Assert.Equal(8, (await SingleAdminAsync()).SessionVersion);
    }

    [Fact]
    public async Task TheCliNeverAppliesMigrations()
    {
        var database = "cli_unmigrated_" + Guid.NewGuid().ToString("N");
        var builder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString);
        await using (var connection = new NpgsqlConnection(builder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {database}", connection);
            await create.ExecuteNonQueryAsync();
        }

        builder.Database = database;
        var exitCode = await RunAsync(builder.ConnectionString, ["list-admins"]);

        // admin_users does not exist (SQLSTATE 42P01) and nothing was created.
        Assert.Equal(3, exitCode);
        Assert.Contains("42P01", _error.ToString(), StringComparison.Ordinal);
        await using (var connection = new NpgsqlConnection(builder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var count = new NpgsqlCommand(
                "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'", connection);
            Assert.Equal(0L, await count.ExecuteScalarAsync());
        }
    }

    private Task<int> RunAsync(params string[] args) => RunAsync(postgres.ConnectionString, args);

    private Task<int> RunAsync(string connectionString, string[] args)
        => CliApplication.RunAsync(args, Configuration(connectionString), _output, _error, CancellationToken.None);

    private Task<int> RunWithOwnWritersAsync(params string[] args)
        => CliApplication.RunAsync(
            args, Configuration(postgres.ConnectionString), new StringWriter(), new StringWriter(), CancellationToken.None);

    private static IConfiguration Configuration(string connectionString)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [SilvaStockApiFactory.ConnectionStringKey] = connectionString,
            })
            .Build();

    private async Task<AdminUser> SingleAdminAsync()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        return Assert.Single(await context.AdminUsers.AsNoTracking().ToListAsync());
    }

    /// <summary>Simulates the first Google login (F2-03) binding a fictitious <c>sub</c>.</summary>
    private async Task BindGoogleAccountAsync(string email, string sub)
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        var updated = await context.Database.ExecuteSqlAsync(
            $"UPDATE admin_users SET google_sub = {sub} WHERE email = {email}");
        Assert.Equal(1, updated);
    }
}
