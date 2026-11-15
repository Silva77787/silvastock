using Microsoft.Extensions.Options;

namespace SilvaStock.Api.Cors;

/// <summary>Validates <see cref="ApiCorsOptions"/> at startup.</summary>
internal sealed class ApiCorsOptionsValidator : IValidateOptions<ApiCorsOptions>
{
    private const string _key = ApiCorsOptions.SectionName + ":" + nameof(ApiCorsOptions.AllowedOrigins);

    public ValidateOptionsResult Validate(string? name, ApiCorsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.AllowedOrigins.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                $"Missing required configuration '{_key}': at least one origin is required " +
                "(e.g. environment variable 'Cors__AllowedOrigins__0=https://www.example.com').");
        }

        var failures = new List<string>();
        foreach (var origin in options.AllowedOrigins)
        {
            if (!IsValidOrigin(origin))
            {
                failures.Add(
                    $"Configuration '{_key}' contains an invalid origin '{origin}'. " +
                    "Use an absolute http(s) origin without path, query or wildcard (e.g. 'https://www.example.com').");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    internal static bool IsValidOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin) || origin.Contains('*', StringComparison.Ordinal))
        {
            return false;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo);
    }
}
