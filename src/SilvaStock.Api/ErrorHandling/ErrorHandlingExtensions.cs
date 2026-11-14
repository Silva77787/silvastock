using System.Diagnostics;

namespace SilvaStock.Api.ErrorHandling;

/// <summary>RFC 9457 ProblemDetails for every error response (unhandled exceptions and empty 4xx/5xx).</summary>
internal static class ErrorHandlingExtensions
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var httpContext = context.HttpContext;
            var problem = context.ProblemDetails;
            var status = problem.Status ?? httpContext.Response.StatusCode;

            problem.Title = ProblemDetailsTitles.Localize(problem.Title, status);
            problem.Instance ??= httpContext.Request.Path;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            // Unhandled exceptions: never leak internal details outside Development.
            if (context.Exception is not null
                && !httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                problem.Detail = null;
                problem.Extensions.Remove("exception");
            }
        });

        return services;
    }

    /// <summary>
    /// Must be the first middleware. Unhandled exceptions become a 500 ProblemDetails (logged by the middleware);
    /// empty error responses (e.g. 404, 405) get a ProblemDetails body. In Development the framework's
    /// developer exception page (also ProblemDetails for non-HTML clients) runs before this handler.
    /// </summary>
    public static WebApplication UseApiErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
