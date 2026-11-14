using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace SilvaStock.Infrastructure.HealthChecks;

/// <summary>
/// Checks PostgreSQL connectivity with the cheapest possible round trip (<c>SELECT 1</c>).
/// Kept deliberately light: Neon suspends compute after inactivity and every call wakes it up.
/// </summary>
internal sealed class PostgresHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    /// <summary>Upper bound for the whole check (connect + query).</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            var connection = await dataSource.OpenConnectionAsync(timeout.Token).ConfigureAwait(false);
            await using (connection.ConfigureAwait(false))
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = "SELECT 1";
                    command.CommandTimeout = (int)Timeout.TotalSeconds;
                    await command.ExecuteScalarAsync(timeout.Token).ConfigureAwait(false);
                }
            }

            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Database check timed out.");
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
        {
            // Npgsql exception messages contain host/port at most, never the password.
            return new HealthCheckResult(context.Registration.FailureStatus, "Database is unreachable.", ex);
        }
    }
}
