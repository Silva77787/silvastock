using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// In-process OpenID Connect provider standing in for Google: discovery document, JWKS with its own RSA signing key
/// and a token endpoint that checks the one-time code, the client credentials, the <c>redirect_uri</c> and PKCE
/// (S256) before returning a signed <c>id_token</c>. The API validates that token for real (signature via JWKS,
/// issuer, audience, lifetime, nonce); nothing in the handler is bypassed. Served through the OIDC backchannel.
/// </summary>
public sealed class FakeOidcIssuer : IDisposable
{
    public const string Issuer = "https://oidc.silvastock.test";
    public const string AuthorizationEndpoint = Issuer + "/authorize";
    public const string TokenEndpoint = Issuer + "/token";
    public const string JwksUri = Issuer + "/jwks";
    public const string MetadataAddress = Issuer + "/.well-known/openid-configuration";

    private readonly RSA _signingRsa = RSA.Create(2048);
    private readonly RSA _unknownRsa = RSA.Create(2048);
    private readonly ConcurrentDictionary<string, PendingCode> _codes = new(StringComparer.Ordinal);

    public FakeOidcIssuer(string clientId, string clientSecret)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        SigningKey = new RsaSecurityKey(_signingRsa) { KeyId = "fake-issuer-key-1" };
        UnknownKey = new RsaSecurityKey(_unknownRsa) { KeyId = "unknown-key-1" };
    }

    public string ClientId { get; }

    public string ClientSecret { get; }

    /// <summary>Published in the JWKS.</summary>
    public RsaSecurityKey SigningKey { get; }

    /// <summary>Never published: tokens signed with it must be rejected.</summary>
    public RsaSecurityKey UnknownKey { get; }

    /// <summary>Number of successful token endpoint calls (code redemptions).</summary>
    public int RedeemedCodes => _redeemed;

    /// <summary>When true the discovery document answers 503 (provider unavailable).</summary>
    public bool MetadataUnavailable { get; set; }

    private int _redeemed;

    /// <summary>Creates the backchannel handler that serves discovery, JWKS and the token endpoint.</summary>
    public HttpMessageHandler CreateHandler() => new Handler(this);

    /// <summary>
    /// Registers a one-time authorization code for <paramref name="request"/> (as Google would after the user
    /// consents) and returns it. <paramref name="token"/> describes the <c>id_token</c> issued for that code.
    /// </summary>
    public string IssueCode(AuthorizationRequest request, FakeIdToken token, string? codeChallengeOverride = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(token);

        var code = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(24));
        _codes[code] = new PendingCode(request, codeChallengeOverride ?? request.CodeChallenge, token);
        return code;
    }

    public void Dispose()
    {
        _signingRsa.Dispose();
        _unknownRsa.Dispose();
    }

    private string CreateIdToken(PendingCode pending)
    {
        var token = pending.Token;
        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["sub"] = token.Subject,
            ["nonce"] = token.Nonce ?? pending.Request.Nonce,
        };

        if (token.Email is not null)
        {
            claims["email"] = token.Email;
        }

        if (token.EmailVerified is not null)
        {
            claims["email_verified"] = token.EmailVerified;
        }

        if (token.Name is not null)
        {
            claims["name"] = token.Name;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = token.Issuer ?? Issuer,
            Audience = token.Audience ?? ClientId,
            IssuedAt = (token.ExpiresAt ?? now.AddMinutes(5)).AddMinutes(-5),
            NotBefore = (token.ExpiresAt ?? now.AddMinutes(5)).AddMinutes(-5),
            Expires = token.ExpiresAt ?? now.AddMinutes(5),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                token.SignWithUnknownKey ? UnknownKey : SigningKey,
                SecurityAlgorithms.RsaSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private HttpResponseMessage HandleToken(IReadOnlyDictionary<string, string> form)
    {
        if (!form.TryGetValue("grant_type", out var grantType) || grantType != "authorization_code"
            || !form.TryGetValue("code", out var code) || !_codes.TryRemove(code, out var pending))
        {
            return Error("invalid_grant");
        }

        if (form.GetValueOrDefault("client_id") != ClientId || form.GetValueOrDefault("client_secret") != ClientSecret)
        {
            return Error("invalid_client");
        }

        if (form.GetValueOrDefault("redirect_uri") != pending.Request.RedirectUri)
        {
            return Error("invalid_grant");
        }

        // PKCE (RFC 7636, S256): BASE64URL(SHA256(code_verifier)) must equal the code_challenge of the request.
        var verifier = form.GetValueOrDefault("code_verifier");
        if (string.IsNullOrEmpty(verifier)
            || Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))) != pending.CodeChallenge)
        {
            return Error("invalid_grant");
        }

        Interlocked.Increment(ref _redeemed);
        return Json(new Dictionary<string, object>
        {
            ["access_token"] = "fake-access-token-" + Guid.NewGuid().ToString("N"),
            ["token_type"] = "Bearer",
            ["expires_in"] = 3600,
            ["id_token"] = CreateIdToken(pending),
        });
    }

    private static Dictionary<string, object> Discovery() => new()
    {
        ["issuer"] = Issuer,
        ["authorization_endpoint"] = AuthorizationEndpoint,
        ["token_endpoint"] = TokenEndpoint,
        ["jwks_uri"] = JwksUri,
        ["response_types_supported"] = new[] { "code" },
        ["subject_types_supported"] = new[] { "public" },
        ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
        ["code_challenge_methods_supported"] = new[] { "S256" },
        ["scopes_supported"] = new[] { "openid", "email", "profile" },
    };

    private Dictionary<string, object> Jwks()
    {
        var parameters = _signingRsa.ExportParameters(includePrivateParameters: false);
        return new Dictionary<string, object>
        {
            ["keys"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["kty"] = "RSA",
                    ["use"] = "sig",
                    ["alg"] = "RS256",
                    ["kid"] = SigningKey.KeyId,
                    ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
                    ["e"] = Base64UrlEncoder.Encode(parameters.Exponent),
                },
            },
        };
    }

    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage Error(string error)
        => Json(new Dictionary<string, string> { ["error"] = error }, HttpStatusCode.BadRequest);

    private sealed record PendingCode(AuthorizationRequest Request, string CodeChallenge, FakeIdToken Token);

    private sealed class Handler(FakeOidcIssuer issuer) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.GetLeftPart(UriPartial.Path);

            if (request.Method == HttpMethod.Get && url == MetadataAddress)
            {
                if (issuer.MetadataUnavailable)
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }

                return Json(Discovery());
            }

            if (request.Method == HttpMethod.Get && url == JwksUri)
            {
                return Json(issuer.Jwks());
            }

            if (request.Method == HttpMethod.Post && url == TokenEndpoint && request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                var form = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pair => pair.Split('=', 2))
                    .ToDictionary(
                        pair => Uri.UnescapeDataString(pair[0].Replace('+', ' ')),
                        pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace('+', ' ')) : string.Empty,
                        StringComparer.Ordinal);
                return issuer.HandleToken(form);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}

/// <summary>The <c>id_token</c> the fake issuer returns for a code. Null optional claims are omitted.</summary>
public sealed record FakeIdToken(string Subject, string? Email)
{
    /// <summary>The <c>email_verified</c> claim value (JSON boolean by default); null omits the claim.</summary>
    public object? EmailVerified { get; init; } = true;

    /// <summary>The <c>name</c> claim; null omits it.</summary>
    public string? Name { get; init; } = "Admin Exemplo";

    /// <summary>Overrides the <c>iss</c> claim (default: <see cref="FakeOidcIssuer.Issuer"/>).</summary>
    public string? Issuer { get; init; }

    /// <summary>Overrides the <c>aud</c> claim (default: the client ID).</summary>
    public string? Audience { get; init; }

    /// <summary>Overrides the <c>nonce</c> claim (default: the nonce of the authorization request).</summary>
    public string? Nonce { get; init; }

    /// <summary>Signs with a key that is not in the JWKS.</summary>
    public bool SignWithUnknownKey { get; init; }

    /// <summary>Overrides the <c>exp</c> claim (default: 5 minutes from now); <c>iat</c>/<c>nbf</c> are 5 minutes earlier.</summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>The authorization request the API redirected the browser to (parsed from the <c>Location</c> header).</summary>
public sealed record AuthorizationRequest(
    Uri Location,
    IReadOnlyDictionary<string, string> Query)
{
    public string State => Query["state"];

    public string Nonce => Query["nonce"];

    public string CodeChallenge => Query["code_challenge"];

    public string RedirectUri => Query["redirect_uri"];

    public static AuthorizationRequest Parse(Uri location)
    {
        ArgumentNullException.ThrowIfNull(location);

        var query = location.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace('+', ' ')) : string.Empty,
                StringComparer.Ordinal);
        return new AuthorizationRequest(location, query);
    }
}
