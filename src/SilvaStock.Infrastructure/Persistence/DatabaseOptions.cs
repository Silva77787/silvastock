namespace SilvaStock.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL settings. The connection string is bound from <c>ConnectionStrings:Default</c>
/// (environment variable <c>ConnectionStrings__Default</c>; user-secrets locally) and must never be logged.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Configuration key the connection string is read from.</summary>
    public const string ConnectionStringKey = "ConnectionStrings:Default";

    /// <summary>Npgsql connection string. Secret: never log or echo it.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Hides the value so the options object can never leak the connection string through logs.</summary>
    public override string ToString() => $"{nameof(DatabaseOptions)} {{ ConnectionString = *** }}";
}
