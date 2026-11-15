using Microsoft.Extensions.Options;
using Npgsql;

namespace SilvaStock.Infrastructure.Persistence;

/// <summary>Validates <see cref="DatabaseOptions"/> at startup. Messages never include the connection string value.</summary>
internal sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"Missing required configuration '{DatabaseOptions.ConnectionStringKey}'. " +
                "Set it with 'dotnet user-secrets set \"ConnectionStrings:Default\" \"<connection string>\"' " +
                "(project src/SilvaStock.Api) or the environment variable 'ConnectionStrings__Default'.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        }
        catch (ArgumentException)
        {
            // Do not include the exception message: it may echo part of the connection string.
            return ValidateOptionsResult.Fail(
                $"Configuration '{DatabaseOptions.ConnectionStringKey}' is not a valid PostgreSQL connection string.");
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            return ValidateOptionsResult.Fail(
                $"Configuration '{DatabaseOptions.ConnectionStringKey}' must specify a Host.");
        }

        return ValidateOptionsResult.Success;
    }
}
