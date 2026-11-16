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
        Assert.DoesNotContain("127.0.0.1", all, StringComparison.Ordinal);
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
