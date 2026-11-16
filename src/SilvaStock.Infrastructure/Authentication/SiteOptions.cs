namespace SilvaStock.Infrastructure.Authentication;

/// <summary>
/// Public site (Next.js) the login flow redirects back to, bound from <c>Site</c> (e.g. <c>Site__BaseUrl</c>).
/// <c>http://localhost:3000</c> in Development, <c>https://www.&lt;domain&gt;</c> in production.
/// </summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    /// <summary>Site origin: scheme, host and optional port, without path, query or fragment.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>The validated <see cref="BaseUrl"/> as an absolute URI (valid only after startup validation).</summary>
    public Uri BaseUri => new(BaseUrl, UriKind.Absolute);
}
