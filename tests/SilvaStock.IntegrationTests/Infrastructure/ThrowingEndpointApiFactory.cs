using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace SilvaStock.IntegrationTests.Infrastructure;

/// <summary>
/// API factory that adds a test-only route which throws, without touching product code. The route
/// is appended through an <see cref="IStartupFilter"/> <b>after</b> the application's own pipeline,
/// so the exception travels back through the product's exception handling middleware.
/// </summary>
public sealed class ThrowingEndpointApiFactory(string connectionString, string environment)
    : SilvaStockApiFactory(connectionString, environment)
{
    public const string ThrowPath = "/__tests/throw";

    /// <summary>Fictitious internal detail that must never reach the client.</summary>
    public const string SecretExceptionMessage = "test-only-internal-detail-7f3c Host=db.internal;Password=not-a-real-secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddTransient<IStartupFilter, ThrowingEndpointStartupFilter>());
    }

    private sealed class ThrowingEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            // Reached only when no product endpoint matched; still inside the product middleware.
            app.Use((HttpContext context, RequestDelegate nextMiddleware) =>
                context.Request.Path.Equals(ThrowPath, StringComparison.Ordinal)
                    ? throw new InvalidOperationException(SecretExceptionMessage)
                    : nextMiddleware(context));
        };
    }
}
