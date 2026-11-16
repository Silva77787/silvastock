using Microsoft.Extensions.Configuration;
using SilvaStock.Cli;

namespace SilvaStock.UnitTests.Cli;

/// <summary>Exit codes that do not need a database (usage and configuration errors).</summary>
public sealed class CliApplicationTests : IDisposable
{
    /// <summary>Fictitious; points at a closed local port, so a connection attempt is refused at once.</summary>
    private const string _unreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=silvastock_unreachable;Username=nobody;Password=secret-test-only-value;Timeout=2;Pooling=false";

    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
    }

    [Fact]
    public async Task NoArgumentsIsInvalidUsageAndPrintsTheUsage()
    {
        var exitCode = await RunAsync([], _unreachableConnectionString);

        Assert.Equal(1, exitCode);
        Assert.Contains("Usage: silvastock", _error.ToString(), StringComparison.Ordinal);
        Assert.Empty(_output.ToString());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("add-admin")]
    [InlineData("add-admin", "not-an-email")]
    [InlineData("remove-admin", "a@example.test", "b@example.test")]
    [InlineData("reset-google")]
    [InlineData("list-admins", "extra")]
    public async Task InvalidUsageReturnsOneWithoutReadingConfiguration(params string[] args)
    {
        // No connection string at all: usage errors are reported before configuration is validated.
        var exitCode = await RunAsync(args, connectionString: null);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Error:", _error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HelpReturnsZeroAndPrintsTheUsage()
    {
        var exitCode = await RunAsync(["help"], connectionString: null);

        Assert.Equal(0, exitCode);
        Assert.Contains("add-admin <email>", _output.ToString(), StringComparison.Ordinal);
        Assert.Contains("reset-google <email>", _output.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("Database=no_host_given;Password=secret-test-only-value")]
    public async Task MissingOrInvalidConnectionStringReturnsThreeWithoutPrintingIt(string? connectionString)
    {
        var exitCode = await RunAsync(["list-admins"], connectionString);

        Assert.Equal(3, exitCode);
        var error = _error.ToString();
        Assert.Contains("Configuration error", error, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:Default", error, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-test-only-value", error, StringComparison.Ordinal);
        Assert.DoesNotContain("no_host_given", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DatabaseErrorReturnsThreeWithoutPrintingTheConnectionString()
    {
        var exitCode = await RunAsync(["list-admins"], _unreachableConnectionString);

        Assert.Equal(3, exitCode);
        var all = _error.ToString() + _output.ToString();
        Assert.Contains("Database error", all, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-test-only-value", all, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nobody", all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryDatabaseCommandNamesTheTargetAndSourceButNeverTheCredentials()
    {
        var exitCode = await RunAsync(["remove-admin", "maria@example.test"], _unreachableConnectionString);

        Assert.Equal(3, exitCode);
        var error = _error.ToString();
        Assert.StartsWith(
            "Target database: Host=127.0.0.1, Port=1, Database=silvastock_unreachable (source: in-memory)",
            error,
            StringComparison.Ordinal);
        Assert.DoesNotContain("secret-test-only-value", error, StringComparison.Ordinal);
        Assert.DoesNotContain("nobody", error, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceIsUserSecretsWhenTheEnvironmentVariableIsMissing()
    {
        var prefix = UniqueEnvironmentPrefix();
        var userSecrets = WriteUserSecretsFile("Host=db.example.test;Database=local;Password=secret-test-only-value");
        try
        {
            // Same order as Program: user-secrets, then environment (wins).
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(userSecrets, optional: false)
                .AddEnvironmentVariables(prefix)
                .Build();

            var target = CliApplication.DescribeTarget(configuration["ConnectionStrings:Default"]!, configuration);

            Assert.Equal("Target database: Host=db.example.test, Port=5432, Database=local (source: user-secrets)", target);
        }
        finally
        {
            File.Delete(userSecrets);
        }
    }

    [Fact]
    public void SourceIsTheEnvironmentVariableWhenSetEvenWithUserSecrets()
    {
        var prefix = UniqueEnvironmentPrefix();
        var userSecrets = WriteUserSecretsFile("Host=localhost;Database=local");
        Environment.SetEnvironmentVariable(
            prefix + "ConnectionStrings__Default",
            "Host=prod.example.test;Port=5433;Database=silvastock;Username=owner;Password=secret-test-only-value");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(userSecrets, optional: false)
                .AddEnvironmentVariables(prefix)
                .Build();

            var target = CliApplication.DescribeTarget(configuration["ConnectionStrings:Default"]!, configuration);

            Assert.Equal(
                "Target database: Host=prod.example.test, Port=5433, Database=silvastock " +
                "(source: environment variable ConnectionStrings__Default)",
                target);
            Assert.DoesNotContain("owner", target, StringComparison.Ordinal);
            Assert.DoesNotContain("secret-test-only-value", target, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "ConnectionStrings__Default", null);
            File.Delete(userSecrets);
        }
    }

    private static string UniqueEnvironmentPrefix() => $"SILVASTOCK_CLI_TEST_{Guid.NewGuid():N}_";

    private static string WriteUserSecretsFile(string connectionString)
    {
        var path = Path.Combine(Path.GetTempPath(), $"silvastock-cli-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, $$"""{ "ConnectionStrings": { "Default": "{{connectionString}}" } }""");
        return path;
    }

    private Task<int> RunAsync(string[] args, string? connectionString)
    {
        var settings = new Dictionary<string, string?>();
        if (connectionString is not null)
        {
            settings["ConnectionStrings:Default"] = connectionString;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return CliApplication.RunAsync(args, configuration, _output, _error, CancellationToken.None);
    }
}
