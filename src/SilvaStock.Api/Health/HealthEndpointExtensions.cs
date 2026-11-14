using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SilvaStock.Infrastructure;

namespace SilvaStock.Api.Health;

/// <summary><c>GET /health</c> as defined in <c>docs/api/openapi.yaml</c>.</summary>
internal static class HealthEndpointExtensions
{
    public const string Path = "/health";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddPostgres();
        return services;
    }

    /// <summary>
    /// Anonymous. 200 <c>{"status":"Healthy"|"Degraded"}</c> or 503 <c>{"status":"Unhealthy"}</c>.
    /// Only the aggregate status is returned: no check names, exceptions or timings.
    /// </summary>
    public static IEndpointConventionBuilder MapApiHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapHealthChecks(Path, new HealthCheckOptions
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
                ResponseWriter = WriteResponseAsync,
            })
            // MapHealthChecks answers every verb by default; the contract only defines GET.
            .WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]))
            .AllowAnonymous()
            .WithName("GetHealth")
            .WithTags("Health");
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthResponse(report.Status.ToString()), context.RequestAborted);

    private sealed record HealthResponse(string Status);
}
