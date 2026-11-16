using Microsoft.Extensions.Options;
using SilvaStock.Application.Auth;

namespace SilvaStock.Infrastructure.Authentication;

/// <summary>Builds every URL the login flow redirects the browser to; always on the configured site.</summary>
public sealed class LoginRedirects(IOptions<SiteOptions> siteOptions)
{
    private Uri SiteBase => siteOptions.Value.BaseUri;

    /// <summary><c>{site}{returnUrl}</c>, with <paramref name="returnUrl"/> sanitised by <see cref="ReturnUrlPolicy"/>.</summary>
    public string ReturnTarget(string? returnUrl) => ReturnUrlPolicy.BuildRedirectUri(returnUrl, SiteBase).AbsoluteUri;

    /// <summary>
    /// Re-checks a target produced by <see cref="ReturnTarget"/> (read back from the protected OIDC state) before
    /// redirecting to it; anything not on the site origin falls back to <c>{site}/admin</c>.
    /// </summary>
    public string RevalidateReturnTarget(string? target)
    {
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, SiteBase.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uri.Host, SiteBase.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == SiteBase.Port
            && string.IsNullOrEmpty(uri.UserInfo))
        {
            return ReturnTarget(uri.PathAndQuery);
        }

        return ReturnTarget(null);
    }

    /// <summary><c>{site}/admin/login?error={code}</c>.</summary>
    public string LoginError(string errorCode)
        => new Uri(SiteBase, $"/admin/login?error={Uri.EscapeDataString(errorCode)}").AbsoluteUri;
}
