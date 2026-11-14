using System.Net;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.ErrorHandling;

/// <summary>Unhandled exceptions become a generic ProblemDetails 500 in Production (RFC 9457).</summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class UnhandledExceptionTests(PostgresFixture postgres)
{
    [Fact]
    public async Task UnhandledExceptionReturnsProblemDetails500WithoutInternalDetailsInProduction()
    {
        await using var factory = new ThrowingEndpointApiFactory(postgres.ConnectionString, "Production");
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(new Uri(ThrowingEndpointApiFactory.ThrowPath, UriKind.Relative));

        var problem = await ProblemDetailsAssert.IsProblemDetailsAsync(response, HttpStatusCode.InternalServerError);
        var body = problem.GetRawText();
        ProblemDetailsAssert.AssertNoInternalDetails(
            body,
            ThrowingEndpointApiFactory.SecretExceptionMessage,
            "test-only-internal-detail-7f3c",
            "not-a-real-secret",
            nameof(InvalidOperationException));
    }
}
