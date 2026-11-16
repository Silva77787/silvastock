using Microsoft.Extensions.Configuration;

namespace SilvaStock.Cli;

/// <summary>
/// Entry point. Allowlist management (DECISIONS section 5, ADR-0004): add-admin, remove-admin, reset-google,
/// list-admins. Not top-level statements, so this <c>Program</c> never clashes with the API's in the test projects.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // ConnectionStrings__Default (environment, wins) or user-secrets; never an argument.
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(CliApplication.UserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            return await CliApplication.RunAsync(args, configuration, Console.Out, Console.Error, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("Cancelled. No changes were saved.");
            return ExitCodes.Failure;
        }
    }
}
