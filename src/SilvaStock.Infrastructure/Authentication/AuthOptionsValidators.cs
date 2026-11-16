using Microsoft.Extensions.Options;

namespace SilvaStock.Infrastructure.Authentication;

/// <summary>Validates <see cref="GoogleAuthOptions"/> at startup. Messages never include the secret.</summary>
internal sealed class GoogleAuthOptionsValidator : IValidateOptions<GoogleAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, GoogleAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add(MissingMessage(nameof(GoogleAuthOptions.ClientId)));
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            failures.Add(MissingMessage(nameof(GoogleAuthOptions.ClientSecret)));
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static string MissingMessage(string property)
    {
        var key = $"{GoogleAuthOptions.SectionName}:{property}";
        return $"Missing required configuration '{key}'. Set it with 'dotnet user-secrets set \"{key}\" \"<value>\"' " +
            $"(project src/SilvaStock.Api) or the environment variable '{key.Replace(":", "__", StringComparison.Ordinal)}'.";
    }
}

/// <summary>Validates <see cref="SiteOptions"/> at startup.</summary>
internal sealed class SiteOptionsValidator : IValidateOptions<SiteOptions>
{
    private const string _key = SiteOptions.SectionName + ":" + nameof(SiteOptions.BaseUrl);

    public ValidateOptionsResult Validate(string? name, SiteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return ValidateOptionsResult.Fail(
                $"Missing required configuration '{_key}' (e.g. environment variable 'Site__BaseUrl=https://www.example.com').");
        }

        return IsValidSiteOrigin(options.BaseUrl)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Configuration '{_key}' must be an absolute https origin without path, query or fragment " +
                "(http is accepted only for localhost), e.g. 'https://www.example.com'.");
    }

    internal static bool IsValidSiteOrigin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var schemeAllowed = uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        return schemeAllowed
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo);
    }
}
