namespace SilvaStock.Application.Auth;

/// <summary>
/// Validates the <c>returnUrl</c> of <c>GET /api/auth/login/google</c> (ADR-0004, Login). Only a relative path equal to
/// <c>/admin</c> or starting with <c>/admin/</c> or <c>/admin?</c> is accepted, at most 512 characters, without control
/// characters, backslashes, <c>//</c> or <c>..</c> segments (also after percent-decoding), and the absolute URL built on
/// the site base must stay on the site's origin. Anything else falls back to <see cref="DefaultReturnUrl"/>.
/// </summary>
public static class ReturnUrlPolicy
{
    /// <summary>Fallback for every rejected value.</summary>
    public const string DefaultReturnUrl = "/admin";

    /// <summary>Maximum accepted length (openapi.yaml: <c>maxLength: 512</c>).</summary>
    public const int MaxLength = 512;

    // Percent-decoding passes; a value that still changes after this many passes is rejected.
    private const int _maxDecodePasses = 3;

    /// <summary>Returns <paramref name="returnUrl"/> when it is acceptable, otherwise <see cref="DefaultReturnUrl"/>.</summary>
    public static string Sanitize(string? returnUrl, Uri siteBaseUrl)
        => IsAllowed(returnUrl, siteBaseUrl) ? returnUrl! : DefaultReturnUrl;

    /// <summary>
    /// Builds the absolute redirect target <c>{site}{returnUrl}</c> from a sanitised <paramref name="returnUrl"/>.
    /// </summary>
    public static Uri BuildRedirectUri(string? returnUrl, Uri siteBaseUrl)
        => new(siteBaseUrl, Sanitize(returnUrl, siteBaseUrl));

    /// <summary>True when <paramref name="returnUrl"/> satisfies every rule of the policy.</summary>
    public static bool IsAllowed(string? returnUrl, Uri siteBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(siteBaseUrl);

        if (string.IsNullOrEmpty(returnUrl) || returnUrl.Length > MaxLength || !siteBaseUrl.IsAbsoluteUri)
        {
            return false;
        }

        if (!HasAdminPrefix(returnUrl) || !IsSafe(returnUrl))
        {
            return false;
        }

        // Every decoded form must be safe too (e.g. "%2F%2F" -> "//", "%5C" -> "\", "%2E%2E" -> "..", "%09" -> tab).
        var current = returnUrl;
        for (var pass = 0; pass < _maxDecodePasses; pass++)
        {
            var decoded = Uri.UnescapeDataString(current);
            if (string.Equals(decoded, current, StringComparison.Ordinal))
            {
                return StaysOnSite(returnUrl, siteBaseUrl);
            }

            if (!HasAdminPrefix(decoded) || !IsSafe(decoded))
            {
                return false;
            }

            current = decoded;
        }

        // Still changing after several passes: nested encoding is never legitimate here.
        return false;
    }

    // Segment-aware: "/admin", "/admin/..." or "/admin?..." ("/adminx" and "/admin#x" are rejected).
    private static bool HasAdminPrefix(string value)
        => value.StartsWith(DefaultReturnUrl, StringComparison.Ordinal)
            && (value.Length == DefaultReturnUrl.Length
                || value[DefaultReturnUrl.Length] == '/'
                || value[DefaultReturnUrl.Length] == '?');

    private static bool IsSafe(string value)
    {
        foreach (var c in value)
        {
            // char.IsControl covers C0 (tab, CR, LF, NUL...), DEL and C1 controls.
            if (char.IsControl(c) || c == '\\')
            {
                return false;
            }
        }

        if (value.Contains("//", StringComparison.Ordinal))
        {
            return false;
        }

        var queryStart = value.IndexOfAny(['?', '#']);
        var path = queryStart < 0 ? value : value[..queryStart];
        foreach (var segment in path.Split('/'))
        {
            if (segment == "..")
            {
                return false;
            }
        }

        return true;
    }

    private static bool StaysOnSite(string returnUrl, Uri siteBaseUrl)
    {
        if (!Uri.TryCreate(siteBaseUrl, returnUrl, out var target))
        {
            return false;
        }

        return string.Equals(target.Scheme, siteBaseUrl.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(target.Host, siteBaseUrl.Host, StringComparison.OrdinalIgnoreCase)
            && target.Port == siteBaseUrl.Port
            && string.IsNullOrEmpty(target.UserInfo)
            && HasAdminPrefix(target.PathAndQuery);
    }
}
