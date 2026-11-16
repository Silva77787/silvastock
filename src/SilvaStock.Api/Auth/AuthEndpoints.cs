using SilvaStock.Infrastructure.Authentication;

namespace SilvaStock.Api.Auth;

/// <summary>
/// Browser-navigation auth endpoints (openapi.yaml, tag Auth). The callback
/// (<c>GET /api/auth/callback/google</c>) is handled by the OIDC authentication middleware, not by an endpoint.
/// </summary>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                AdminAuthenticationDefaults.LoginPath,
                (string? returnUrl, HttpContext httpContext, GoogleLoginStarter starter) => starter.StartAsync(httpContext, returnUrl))
            .AllowAnonymous()
            .WithTags("Auth");

        return app;
    }
}
