using System.Net;
using System.Text.Json;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Health;

/// <summary><c>GET /health</c> with a reachable database (openapi.yaml: 200, <c>{ status }</c>).</summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class HealthEndpointTests(PostgresFixture postgres)
{
    [Fact]
    public async Task HealthReturnsHealthyWhenDatabaseIsReachable()
    {
        await using var factory = postgres.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Healthy", HealthResponse.ReadStatus(body));
        HealthResponse.AssertNoConnectionDetails(body, factory.ConnectionString);
    }
}

/// <summary><c>GET /health</c> with an unreachable database (openapi.yaml: 503). Needs no Docker.</summary>
public sealed class HealthEndpointUnhealthyTests
{
    [Fact]
    public async Task HealthReturns503UnhealthyWhenDatabaseIsUnreachable()
    {
        await using var factory = SilvaStockApiFactory.WithUnreachableDatabase();
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", HealthResponse.ReadStatus(body));
        HealthResponse.AssertNoConnectionDetails(body, factory.ConnectionString);
        ProblemDetailsAssert.AssertNoInternalDetails(body, "Npgsql", "SocketException");
    }
}

internal static class HealthResponse
{
    public static string? ReadStatus(string body)
    {
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("status", out var status), "Health response must contain 'status'.");
        return status.GetString();
    }

    /// <summary>NFR-14: <c>/health</c> must not leak connection details or credentials.</summary>
    public static void AssertNoConnectionDetails(string body, string connectionString)
    {
        Assert.DoesNotContain(connectionString, body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", body, StringComparison.OrdinalIgnoreCase);
    }
}
