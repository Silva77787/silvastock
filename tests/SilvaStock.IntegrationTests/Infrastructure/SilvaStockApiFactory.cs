using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SilvaStock.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory host for SilvaStock.Api. The connection string is injected under
/// <c>ConnectionStrings:Default</c> both as a host setting (visible to configuration read eagerly
/// in <c>Program.cs</c>) and as the last configuration source (wins over appsettings/user-secrets).
/// </summary>
public class SilvaStockApiFactory(string connectionString, string environment = SilvaStockApiFactory.TestingEnvironment)
    : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";

    public const string ConnectionStringKey = "ConnectionStrings:Default";

    /// <summary>Required at startup by the API's CORS options validation.</summary>
    public const string CorsOriginKey = "Cors:AllowedOrigins:0";

    /// <summary>Fictitious site origin used by tests (reserved <c>.test</c> TLD).</summary>
    public const string TestSiteOrigin = "https://www.silvastock.test";

    /// <summary>Required at startup by the API's site options validation (login redirects).</summary>
    public const string SiteBaseUrlKey = "Site:BaseUrl";

    /// <summary>Required at startup by the Google OAuth client options validation.</summary>
    public const string GoogleClientIdKey = "Authentication:Google:ClientId";

    /// <inheritdoc cref="GoogleClientIdKey"/>
    public const string GoogleClientSecretKey = "Authentication:Google:ClientSecret";

    /// <summary>Fictitious Google OAuth client (never a real client).</summary>
    public const string TestGoogleClientId = "silvastock-test-client.apps.example.test";

    /// <inheritdoc cref="TestGoogleClientId"/>
    public const string TestGoogleClientSecret = "test-only-client-secret";

    /// <summary>
    /// A syntactically valid connection string that points at a closed local port, so every
    /// connection attempt is refused quickly. Fictitious credentials.
    /// </summary>
    public const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=silvastock_unreachable;Username=nobody;Password=unreachable-test-only;Timeout=2;Command Timeout=2;Pooling=false";

    public string ConnectionString { get; } = connectionString;

    /// <summary>Factory whose database cannot be reached (for the unhealthy case).</summary>
    public static SilvaStockApiFactory WithUnreachableDatabase(string environment = TestingEnvironment)
        => new(UnreachableConnectionString, environment);

    /// <summary>HTTPS client without automatic redirects (Secure cookies, explicit 3xx assertions).</summary>
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(environment);
        builder.UseSetting(ConnectionStringKey, ConnectionString);
        builder.UseSetting(CorsOriginKey, TestSiteOrigin);
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConnectionStringKey] = ConnectionString,
                [CorsOriginKey] = TestSiteOrigin,
                [SiteBaseUrlKey] = TestSiteOrigin,
                [GoogleClientIdKey] = TestGoogleClientId,
                [GoogleClientSecretKey] = TestGoogleClientSecret,
            }));
    }
}
