using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SilvaStock.Infrastructure.Authentication;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// API host whose Google scheme talks to <see cref="FakeOidcIssuer"/> (authority + backchannel only; every other OIDC
/// setting is the product's). Adds test-only probes after the product pipeline: <see cref="SessionProbePath"/> echoes
/// the session claims (or challenges) and <see cref="ForbidProbePath"/> forbids. Captures every log message.
/// </summary>
public sealed class GoogleLoginApiFactory : SilvaStockApiFactory
{
    public const string SessionProbePath = "/__tests/session";
    public const string ForbidProbePath = "/__tests/forbid";

    public GoogleLoginApiFactory(string connectionString)
        : base(connectionString)
    {
        Issuer = new FakeOidcIssuer(TestGoogleClientId, TestGoogleClientSecret);
    }

    public FakeOidcIssuer Issuer { get; }

    /// <summary>Every formatted log message written by the API (all categories).</summary>
    public ConcurrentQueue<string> Logs { get; } = new();

    /// <summary>HTTPS client without automatic redirects or a cookie container: cookies are forwarded explicitly.</summary>
    public HttpClient CreateBrowserClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = false,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureLogging(logging =>
        {
            // Provider-specific rule: captures Information and above from every category, whatever appsettings says.
            logging.AddFilter<CapturingLoggerProvider>(category: null, LogLevel.Information);
            logging.AddProvider(new CapturingLoggerProvider(Logs));
        });

        builder.ConfigureTestServices(services =>
        {
            services.Configure<OpenIdConnectOptions>(AdminAuthenticationDefaults.GoogleScheme, options =>
            {
                options.Authority = FakeOidcIssuer.Issuer;
                options.BackchannelHttpHandler = Issuer.CreateHandler();
            });
            services.AddTransient<IStartupFilter, ProbeStartupFilter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Issuer.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class ProbeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            // Reached only when no product endpoint matched; authentication has already run.
            app.Use(async (HttpContext context, RequestDelegate nextMiddleware) =>
            {
                if (context.Request.Path.Equals(SessionProbePath, StringComparison.Ordinal))
                {
                    if (context.User.Identity?.IsAuthenticated != true)
                    {
                        await context.ChallengeAsync();
                        return;
                    }

                    await context.Response.WriteAsJsonAsync(
                        context.User.Claims.ToDictionary(c => c.Type, c => c.Value, StringComparer.Ordinal));
                    return;
                }

                if (context.Request.Path.Equals(ForbidProbePath, StringComparison.Ordinal))
                {
                    await context.ForbidAsync();
                    return;
                }

                await nextMiddleware(context);
            });
        };
    }

    private sealed class CapturingLoggerProvider(ConcurrentQueue<string> sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, sink);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string category, ConcurrentQueue<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                sink.Enqueue(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{logLevel} {category}: {formatter(state, exception)} {exception}"));
            }
        }
    }
}
