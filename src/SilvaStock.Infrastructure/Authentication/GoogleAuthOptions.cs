namespace SilvaStock.Infrastructure.Authentication;

/// <summary>
/// Google OAuth client (ADR-0004), bound from <c>Authentication:Google</c>. Both values are set with
/// <c>dotnet user-secrets</c> locally (project <c>src/SilvaStock.Api</c>) and as secrets in production
/// (<c>Authentication__Google__ClientId</c>, <c>Authentication__Google__ClientSecret</c>); never in appsettings.
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>OAuth client ID (audience of the <c>id_token</c>).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client secret. Secret: never log or echo it.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Hides the secret so the options object can never leak it through logs.</summary>
    public override string ToString() => $"{nameof(GoogleAuthOptions)} {{ ClientId = {ClientId}, ClientSecret = *** }}";
}
