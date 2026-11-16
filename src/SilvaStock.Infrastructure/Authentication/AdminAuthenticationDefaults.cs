namespace SilvaStock.Infrastructure.Authentication;

/// <summary>Scheme names, cookie name and paths of the admin login (ADR-0004, openapi.yaml).</summary>
public static class AdminAuthenticationDefaults
{
    /// <summary>Cookie session scheme: default authenticate, challenge, forbid and sign-in scheme.</summary>
    public const string SessionScheme = "AdminSession";

    /// <summary>Google OpenID Connect scheme. Never a default scheme: only challenged by the login endpoint.</summary>
    public const string GoogleScheme = "Google";

    /// <summary>Session cookie (host-only, Secure, HttpOnly, SameSite=Lax, Path=/).</summary>
    public const string SessionCookieName = "__Host-silvastock-auth";

    public const string GoogleAuthority = "https://accounts.google.com";

    public const string LoginPath = "/api/auth/login/google";

    public const string CallbackPath = "/api/auth/callback/google";

    /// <summary>Sliding session lifetime (renewed after half of the window).</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    /// <summary>Lifetime of the OIDC correlation and nonce cookies (time allowed to complete the Google login).</summary>
    public static readonly TimeSpan RemoteLoginTimeout = TimeSpan.FromMinutes(10);
}

/// <summary>Claims stored in the session cookie (ADR-0004, Session). No tokens.</summary>
public static class SessionClaimTypes
{
    public const string AdminId = "admin_id";

    public const string Email = "email";

    public const string SessionVersion = "session_version";

    /// <summary>Google <c>name</c> claim: display-name fallback, never persisted.</summary>
    public const string Name = "name";

    /// <summary>Login time in Unix seconds (UTC); basis of the 90-day absolute maximum.</summary>
    public const string IssuedAt = "issued_at";
}

/// <summary>Error codes sent to <c>{site}/admin/login?error=</c> (openapi.yaml).</summary>
public static class LoginErrorCodes
{
    public const string NotAllowed = "not_allowed";

    public const string RateLimited = "rate_limited";

    public const string LoginFailed = "login_failed";
}
