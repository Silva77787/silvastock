using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using SilvaStock.Infrastructure.HealthChecks;
using SilvaStock.Infrastructure.Persistence;

namespace SilvaStock.Infrastructure;

/// <summary>Composition of Infrastructure services.</summary>
public static class DependencyInjection
{
    /// <summary>Health-check tag for checks that depend on the database.</summary>
    public const string DatabaseTag = "database";

    /// <summary>
    /// Registers database options (validated at startup), the shared <see cref="NpgsqlDataSource"/> and
    /// <see cref="SilvaStockDbContext"/>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration[DatabaseOptions.ConnectionStringKey] ?? string.Empty)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        // Built lazily on first use; options are validated at startup, before any request.
        services.AddSingleton(sp =>
            NpgsqlDataSource.Create(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        // Shares the validated data source above; migrations are never applied here (explicit deploy step).
        services.AddDbContext<SilvaStockDbContext>((sp, options) =>
            SilvaStockDbContext.Configure(options, sp.GetRequiredService<NpgsqlDataSource>()));

        return services;
    }

    /// <summary>
    /// Adds the PostgreSQL connectivity check (<c>SELECT 1</c>). The 3 s timeout is enforced inside the check
    /// so a timeout is reported as a plain Unhealthy result rather than an unhandled exception.
    /// </summary>
    public static IHealthChecksBuilder AddPostgres(this IHealthChecksBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddCheck<PostgresHealthCheck>(
            "postgres",
            failureStatus: HealthStatus.Unhealthy,
            tags: [DatabaseTag]);
    }
}
