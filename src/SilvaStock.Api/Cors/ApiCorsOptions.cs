namespace SilvaStock.Api.Cors;

/// <summary>CORS settings, bound from the <c>Cors</c> section (e.g. <c>Cors__AllowedOrigins__0</c>).</summary>
public sealed class ApiCorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Exact origins of the site/back office (scheme + host + optional port). Required, no wildcards.</summary>
    public IList<string> AllowedOrigins { get; } = [];
}
