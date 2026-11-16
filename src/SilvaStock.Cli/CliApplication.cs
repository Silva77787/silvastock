using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using SilvaStock.Application.AdminUsers.Management;
using SilvaStock.Infrastructure;
using SilvaStock.Infrastructure.AdminUsers;
using SilvaStock.Infrastructure.Persistence;

namespace SilvaStock.Cli;

/// <summary>
/// Runs one CLI command: parses the arguments, wires the services and maps each outcome to a message and an exit
/// code. Never applies migrations and never prints the connection string or exception messages that may contain it.
/// </summary>
internal static class CliApplication
{
    /// <summary>Same user-secrets store as the API, so local development configures the connection string once.</summary>
    public const string UserSecretsId = "silvastock-api";

    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        IConfiguration configuration,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var parsed = CommandLineParser.Parse(args);
        if (parsed.Command is null)
        {
            await error.WriteLineAsync($"Error: {parsed.Error}");
            await error.WriteLineAsync();
            await error.WriteLineAsync(CommandLineParser.Usage);
            return ExitCodes.InvalidUsage;
        }

        if (parsed.Command is CliCommand.Help)
        {
            await output.WriteLineAsync(CommandLineParser.Usage);
            return ExitCodes.Success;
        }

        var services = new ServiceCollection()
            .AddInfrastructure(configuration)
            .AddAdminUserManagement();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });

        try
        {
            // Validates the connection string; the messages never include its value.
            _ = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        }
        catch (OptionsValidationException ex)
        {
            foreach (var failure in ex.Failures)
            {
                await error.WriteLineAsync($"Configuration error: {failure}");
            }

            return ExitCodes.Failure;
        }

        await using var scope = provider.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<AdminUserManagementService>();

        try
        {
            return parsed.Command switch
            {
                CliCommand.AddAdmin add => await AddAdminAsync(management, add, output, error, cancellationToken),
                CliCommand.RemoveAdmin remove => await RemoveAdminAsync(management, remove, output, error, cancellationToken),
                CliCommand.ResetGoogle reset => await ResetGoogleAsync(management, reset, output, error, cancellationToken),
                CliCommand.ListAdmins => await ListAdminsAsync(management, output, cancellationToken),
                _ => throw new InvalidOperationException("Unhandled command."),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Exception messages are not printed: connection errors can echo host or connection details.
            var detail = ex is PostgresException postgres
                ? $"{ex.GetType().Name}, SQLSTATE {postgres.SqlState}"
                : ex.GetType().Name;
            await error.WriteLineAsync($"Database error ({detail}). No changes were saved.");
            return ExitCodes.Failure;
        }
    }

    private static async Task<int> AddAdminAsync(
        AdminUserManagementService management,
        CliCommand.AddAdmin command,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var result = await management.AddAdminAsync(command.Email, command.DisplayName, cancellationToken);
        var message = result.Outcome switch
        {
            AddAdminOutcome.Created => $"Created administrator {result.Email}.",
            AddAdminOutcome.Reactivated => $"Reactivated administrator {result.Email}.",
            AddAdminOutcome.DisplayNameUpdated => $"Administrator {result.Email} is already active; display name updated.",
            AddAdminOutcome.AlreadyActive => $"Administrator {result.Email} is already active; nothing to change.",
            _ => null,
        };

        if (message is null)
        {
            await error.WriteLineAsync(result.Outcome == AddAdminOutcome.InvalidEmail
                ? "Error: invalid email address."
                : "Error: invalid name.");
            return ExitCodes.InvalidUsage;
        }

        await output.WriteLineAsync(message);
        return ExitCodes.Success;
    }

    private static async Task<int> RemoveAdminAsync(
        AdminUserManagementService management,
        CliCommand.RemoveAdmin command,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var result = await management.RemoveAdminAsync(command.Email, cancellationToken);
        switch (result.Outcome)
        {
            case RemoveAdminOutcome.Deactivated:
                await output.WriteLineAsync($"Deactivated administrator {result.Email}; every session was revoked.");
                return ExitCodes.Success;
            case RemoveAdminOutcome.AlreadyInactive:
                await output.WriteLineAsync($"Administrator {result.Email} was already inactive; every session was revoked.");
                return ExitCodes.Success;
            case RemoveAdminOutcome.NotFound:
                await error.WriteLineAsync($"Error: no administrator with email {result.Email}.");
                return ExitCodes.NotFound;
            default:
                await error.WriteLineAsync("Error: invalid email address.");
                return ExitCodes.InvalidUsage;
        }
    }

    private static async Task<int> ResetGoogleAsync(
        AdminUserManagementService management,
        CliCommand.ResetGoogle command,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var result = await management.ResetGoogleAsync(command.Email, cancellationToken);
        switch (result.Outcome)
        {
            case ResetGoogleOutcome.Reset:
                await output.WriteLineAsync(
                    $"Unbound the Google account of {result.Email}; every session was revoked. " +
                    "The next Google login with this email binds the new account.");
                return ExitCodes.Success;
            case ResetGoogleOutcome.NoGoogleAccountBound:
                await output.WriteLineAsync(
                    $"Administrator {result.Email} had no Google account bound; every session was revoked.");
                return ExitCodes.Success;
            case ResetGoogleOutcome.NotFound:
                await error.WriteLineAsync($"Error: no administrator with email {result.Email}.");
                return ExitCodes.NotFound;
            default:
                await error.WriteLineAsync("Error: invalid email address.");
                return ExitCodes.InvalidUsage;
        }
    }

    private static async Task<int> ListAdminsAsync(
        AdminUserManagementService management,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var admins = await management.ListAdminsAsync(cancellationToken);
        if (admins.Count == 0)
        {
            await output.WriteLineAsync("No administrators.");
            return ExitCodes.Success;
        }

        string[] header = ["EMAIL", "NAME", "ACTIVE", "LAST LOGIN (UTC)", "GOOGLE BOUND"];
        var rows = admins
            .Select(a => new[]
            {
                a.Email,
                a.DisplayName ?? "-",
                a.IsActive ? "yes" : "no",
                a.LastLoginAt?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "never",
                a.HasGoogleAccount ? "yes" : "no",
            })
            .ToList();

        var widths = header
            .Select((title, column) => Math.Max(title.Length, rows.Max(r => r[column].Length)))
            .ToArray();

        await output.WriteLineAsync(FormatRow(header, widths));
        foreach (var row in rows)
        {
            await output.WriteLineAsync(FormatRow(row, widths));
        }

        return ExitCodes.Success;
    }

    private static string FormatRow(string[] cells, int[] widths)
        => string.Join("  ", cells.Select((cell, column) => cell.PadRight(widths[column]))).TrimEnd();
}
