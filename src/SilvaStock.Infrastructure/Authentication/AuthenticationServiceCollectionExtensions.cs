using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SilvaStock.Application.Auth;
using SilvaStock.Infrastructure.Persistence;

namespace SilvaStock.Infrastructure.Authentication;

/// <summary>Google OIDC login + cookie session (ADR-0004). Registered by the API only (the CLI does not sign in).</summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the validated <see cref="GoogleAuthOptions"/> and <see cref="SiteOptions"/>, the sign-in use case,
    /// the session cookie scheme (default scheme for everything) and the Google OIDC scheme (challenged explicitly only).
    /// </summary>
    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<GoogleAuthOptions>, GoogleAuthOptionsValidator>();

        services.AddOptions<SiteOptions>()
            .Bind(configuration.GetSection(SiteOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SiteOptions>, SiteOptionsValidator>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAdminLoginStore, AdminLoginStore>();
        services.AddScoped<GoogleSignInService>();
        services.AddSingleton<LoginRedirects>();
        services.AddScoped<GoogleOidcEvents>();
        services.AddSingleton<SessionCookieEvents>();

        services
            .AddAuthentication(options =>
            {
                // The OIDC handler is never a default scheme: an API challenge must answer 401, not redirect to Google.
                options.DefaultScheme = AdminAuthenticationDefaults.SessionScheme;
                options.DefaultChallengeScheme = AdminAuthenticationDefaults.SessionScheme;
                options.DefaultForbidScheme = AdminAuthenticationDefaults.SessionScheme;
                options.DefaultSignInScheme = AdminAuthenticationDefaults.SessionScheme;
                options.DefaultSignOutScheme = AdminAuthenticationDefaults.SessionScheme;
            })
            .AddCookie(AdminAuthenticationDefaults.SessionScheme, options =>
            {
                options.Cookie.Name = AdminAuthenticationDefaults.SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.Cookie.Domain = null;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = AdminAuthenticationDefaults.SessionLifetime;
                options.SlidingExpiration = true;
                options.EventsType = typeof(SessionCookieEvents);
            })
            .AddOpenIdConnect(AdminAuthenticationDefaults.GoogleScheme, ConfigureGoogle);

        services.AddOptions<OpenIdConnectOptions>(AdminAuthenticationDefaults.GoogleScheme)
            .Configure<IOptions<GoogleAuthOptions>>((options, google) =>
            {
                options.ClientId = google.Value.ClientId;
                options.ClientSecret = google.Value.ClientSecret;
            });

        return services;
    }

    private static void ConfigureGoogle(OpenIdConnectOptions options)
    {
        options.Authority = AdminAuthenticationDefaults.GoogleAuthority;
        options.RequireHttpsMetadata = true;
        options.CallbackPath = AdminAuthenticationDefaults.CallbackPath;
        options.SignInScheme = AdminAuthenticationDefaults.SessionScheme;

        // Authorization code flow with PKCE, GET callback; no tokens kept, no userinfo call.
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.UsePkce = true;
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");

        // Issuer (from the discovery document; Google also documents the scheme-less form) and audience (client ID)
        // are validated by the handler.
        options.TokenValidationParameters.ValidIssuers = [AdminAuthenticationDefaults.GoogleAuthority, "accounts.google.com"];
        options.TokenValidationParameters.NameClaimType = "name";

        // No OIDC sign-out flow: an enabled /signout-oidc would let any site delete the session cookie.
        options.RemoteSignOutPath = PathString.Empty;
        options.SignedOutCallbackPath = PathString.Empty;

        // Correlation and nonce cookies: always Secure, short-lived. Lax is enough for the top-level GET callback.
        options.RemoteAuthenticationTimeout = AdminAuthenticationDefaults.RemoteLoginTimeout;
        options.ProtocolValidator.NonceLifetime = AdminAuthenticationDefaults.RemoteLoginTimeout;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.HttpOnly = true;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.HttpOnly = true;
        options.NonceCookie.Expiration = AdminAuthenticationDefaults.RemoteLoginTimeout;

        options.DisableTelemetry = true;
        options.EventsType = typeof(GoogleOidcEvents);
    }
}
