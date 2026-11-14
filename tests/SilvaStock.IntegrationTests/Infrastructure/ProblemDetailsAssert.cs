using System.Net;
using System.Text.Json;

namespace SilvaStock.IntegrationTests.Infrastructure;

/// <summary>Assertions for RFC 9457 ProblemDetails responses (see <c>ProblemDetails</c> in openapi.yaml).</summary>
public static class ProblemDetailsAssert
{
    public const string ProblemJsonMediaType = "application/problem+json";

    /// <summary>Markers that must never appear in a response body (exception/stack trace leakage).</summary>
    private static readonly string[] _internalDetailMarkers =
    [
        "   at ",
        "StackTrace",
        "stackTrace",
        "Exception:",
        "System.InvalidOperationException",
        "SilvaStock.IntegrationTests",
        ".cs:line",
        "innerException",
    ];

    /// <summary>
    /// Asserts the response is a ProblemDetails with the expected status, a <c>traceId</c> and no
    /// internal details. Returns the parsed body for further assertions.
    /// </summary>
    public static async Task<JsonElement> IsProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        ArgumentNullException.ThrowIfNull(response);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement.Clone();

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("status", out var status), "ProblemDetails must contain 'status'.");
        Assert.Equal((int)expectedStatus, status.GetInt32());
        Assert.True(root.TryGetProperty("title", out var title), "ProblemDetails must contain 'title'.");
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));
        Assert.True(root.TryGetProperty("traceId", out var traceId), "ProblemDetails must contain 'traceId'.");
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));

        AssertNoInternalDetails(body);
        return root;
    }

    /// <summary>Asserts the body leaks no exception type, message fragment or stack trace.</summary>
    public static void AssertNoInternalDetails(string body, params string[] forbiddenFragments)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(forbiddenFragments);

        foreach (var marker in _internalDetailMarkers.Concat(forbiddenFragments))
        {
            Assert.DoesNotContain(marker, body, StringComparison.Ordinal);
        }

        using var document = JsonDocument.Parse(body);
        Assert.False(document.RootElement.TryGetProperty("exception", out _), "ProblemDetails must not contain 'exception'.");
        Assert.False(document.RootElement.TryGetProperty("exceptionDetails", out _), "ProblemDetails must not contain 'exceptionDetails'.");
    }
}
