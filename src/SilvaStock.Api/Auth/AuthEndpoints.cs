using Microsoft.AspNetCore.Authentication;
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
        app.MapGet(AdminAuthenticationDefaults.LoginPath, StartGoogleLogin)
            .AllowAnonymous()
            .WithTags("Auth");

        return app;
    }

    /// <summary>
    /// Challenges the Google scheme explicitly. The sanitised <c>{site}{returnUrl}</c> travels only inside the
    /// protected OIDC state and is used only after a successful login.
    /// </summary>
    private static IResult StartGoogleLogin(string? returnUrl, LoginRedirects redirects)
        => Results.Challenge(
            new AuthenticationProperties { RedirectUri = redirects.ReturnTarget(returnUrl) },
            [AdminAuthenticationDefaults.GoogleScheme]);
}
