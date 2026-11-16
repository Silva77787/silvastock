using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SilvaStock.Application.Auth;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// Remote failures of the Google callback (ADR-0004 Login): cancelled at Google, bad state, missing correlation,
/// and tokens that fail real validation (nonce, signature, issuer, audience, PKCE) all end at
/// <c>{site}/admin/login?error=login_failed</c> with no session and nothing reflected.
/// </summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class GoogleLoginFailureTests(PostgresFixture postgres) : GoogleLoginTestBase(postgres)
{
    private const string _evilText = "evil-error-description-<script>alert(1)</script>";

    [Theory]
    [InlineData("access_denied")]
    [InlineData("server_error")]
    public async Task ErrorReturnedByGoogleIsLoginFailedWithoutReflectingIt(string error)
    {
        await SeedAdminAsync();
        var start = await StartLoginAsync("/admin/veiculos");

        using var response = await CallbackAsync(
            $"error={error}&error_description={Uri.EscapeDataString(_evilText)}&error_uri={Uri.EscapeDataString("https://evil.example.test/")}" +
            $"&state={Uri.EscapeDataString(start.Request.State)}",
            start.CookieHeader);

        await AssertLoginFailedAsync(response);
    }

    [Theory]
    [InlineData(SilvaStockApiFactory.TestingEnvironment, "access_denied")]
    [InlineData(SilvaStockApiFactory.TestingEnvironment, "server_error")]
    [InlineData("Development", "access_denied")]
    [InlineData("Development", "server_error")]
    [InlineData("Production", "server_error")]
    public async Task ErrorTextSentToTheCallbackIsNeverLogged(string environment, string error)
    {
        await UseFactoryAsync(Postgres.ConnectionString, environment);
        await SeedAdminAsync();
        var start = await StartLoginAsync();

        using var response = await CallbackAsync(
            $"error={error}&error_description={Uri.EscapeDataString(_evilText)}&error_uri={Uri.EscapeDataString("https://evil.example.test/")}" +
            $"&state={Uri.EscapeDataString(start.Request.State)}",
            start.CookieHeader);

        await AssertLoginFailedAsync(response);
        var logs = Factory.Logs.ToList();
        Assert.Contains(logs, l => l.Contains("Google login failed: login_failed", StringComparison.Ordinal));
        Assert.All(logs, l => Assert.DoesNotContain("evil", l, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExpiredIdTokenIsLoginFailed()
    {
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail) { ExpiresAt = DateTime.UtcNow.AddMinutes(-10) });

        await AssertLoginFailedAsync(response);
        Assert.Equal(1, Factory.Issuer.RedeemedCodes);
    }

    [Fact]
    public async Task LoginIsLoginFailedWhenTheIssuerMetadataIsUnavailable()
    {
        Factory.Issuer.MetadataUnavailable = true;

        using var response = await Client.GetAsync(new Uri($"{LoginPath}?returnUrl=%2Fadmin%2Fveiculos", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(LoginFailedLocation, response.Headers.Location?.AbsoluteUri);
        Assert.Empty(SetCookies(response));
        Assert.DoesNotContain("oidc.silvastock.test", body, StringComparison.Ordinal);
        Assert.Contains(Factory.Logs, l => l.Contains("Google login could not start: login_failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TamperedStateIsLoginFailed()
    {
        await SeedAdminAsync();
        var start = await StartLoginAsync();
        var code = Factory.Issuer.IssueCode(start.Request, new FakeIdToken(GoogleSub, AdminEmail));

        using var response = await CallbackAsync(CallbackQuery(code, start.Request.State + "x"), start.CookieHeader);

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task ForeignStateIsLoginFailed()
    {
        await SeedAdminAsync();

        using var response = await CallbackAsync(CallbackQuery("some-code", _evilText), cookieHeader: null);

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task CallbackWithoutParametersIsLoginFailed()
    {
        using var response = await CallbackAsync(string.Empty, cookieHeader: null);

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task MissingCorrelationAndNonceCookiesIsLoginFailed()
    {
        await SeedAdminAsync();
        var start = await StartLoginAsync();
        var code = Factory.Issuer.IssueCode(start.Request, new FakeIdToken(GoogleSub, AdminEmail));

        // Valid state and code, but sent from a browser that did not start the login (login CSRF).
        using var response = await CallbackAsync(CallbackQuery(code, start.Request.State), cookieHeader: null);

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task NonceMismatchIsLoginFailed()
    {
        await SeedAdminAsync();
        var start = await StartLoginAsync();
        var code = Factory.Issuer.IssueCode(start.Request, new FakeIdToken(GoogleSub, AdminEmail) { Nonce = "another-nonce" });

        using var response = await CallbackAsync(CallbackQuery(code, start.Request.State), start.CookieHeader);

        await AssertLoginFailedAsync(response);
    }

    public static TheoryData<string, FakeIdToken> InvalidTokens => new()
    {
        { "signed with a key outside the JWKS", new FakeIdToken(GoogleSub, AdminEmail) { SignWithUnknownKey = true } },
        { "wrong issuer", new FakeIdToken(GoogleSub, AdminEmail) { Issuer = "https://evil.example.test" } },
        { "wrong audience", new FakeIdToken(GoogleSub, AdminEmail) { Audience = "another-client.apps.example.test" } },
    };

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task IdTokenThatFailsValidationIsLoginFailed(string description, FakeIdToken token)
    {
        Assert.NotNull(description);
        await SeedAdminAsync();

        using var response = await LoginAsync(token);

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task PkceVerifierThatDoesNotMatchTheChallengeIsLoginFailed()
    {
        await SeedAdminAsync();
        var start = await StartLoginAsync();
        var code = Factory.Issuer.IssueCode(
            start.Request,
            new FakeIdToken(GoogleSub, AdminEmail),
            codeChallengeOverride: "aW52YWxpZC1jaGFsbGVuZ2UtdmFsdWUtZm9yLXRlc3Rz");

        using var response = await CallbackAsync(CallbackQuery(code, start.Request.State), start.CookieHeader);

        await AssertLoginFailedAsync(response);
        Assert.Equal(0, Factory.Issuer.RedeemedCodes);
    }

    [Fact]
    public async Task AllowlistLookupFailureIsLoginFailed()
    {
        await using var factory = new GoogleLoginApiFactory(SilvaStockApiFactory.UnreachableConnectionString);
        using var client = factory.CreateBrowserClient();

        using var start = await client.GetAsync(new Uri(LoginPath, UriKind.Relative));
        var request = AuthorizationRequest.Parse(start.Headers.Location!);
        var code = factory.Issuer.IssueCode(request, new FakeIdToken(GoogleSub, AdminEmail));
        using var callback = new HttpRequestMessage(HttpMethod.Get, new Uri($"{CallbackPath}?{CallbackQuery(code, request.State)}", UriKind.Relative));
        callback.Headers.Add("Cookie", string.Join("; ", SetCookies(start).Select(c => $"{c.Name}={c.Value}")));

        using var response = await client.SendAsync(callback);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(LoginFailedLocation, response.Headers.Location?.AbsoluteUri);
        AssertNoSessionCookie(response);
        Assert.Equal(1, factory.Issuer.RedeemedCodes);
    }

    [Fact]
    public async Task CancellationNotCausedByTheClientIsLoginFailed()
    {
        // E.g. a database command timeout surfacing as OperationCanceledException while the request is still live.
        await using var factory = new GoogleLoginApiFactory(Postgres.ConnectionString)
        {
            ExtraServices = services => services.AddScoped<IAdminLoginStore, CancellingStore>(),
        };
        using var client = factory.CreateBrowserClient();

        using var start = await client.GetAsync(new Uri(LoginPath, UriKind.Relative));
        var request = AuthorizationRequest.Parse(start.Headers.Location!);
        var code = factory.Issuer.IssueCode(request, new FakeIdToken(GoogleSub, AdminEmail));
        using var callback = new HttpRequestMessage(HttpMethod.Get, new Uri($"{CallbackPath}?{CallbackQuery(code, request.State)}", UriKind.Relative));
        callback.Headers.Add("Cookie", string.Join("; ", SetCookies(start).Select(c => $"{c.Name}={c.Value}")));

        using var response = await client.SendAsync(callback);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(LoginFailedLocation, response.Headers.Location?.AbsoluteUri);
        AssertNoSessionCookie(response);
        Assert.Contains(factory.Logs, l => l.Contains("login_failed (OperationCanceledException)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CodeCannotBeReplayed()
    {
        await SeedAdminAsync();
        var start = await StartLoginAsync();
        var code = Factory.Issuer.IssueCode(start.Request, new FakeIdToken(GoogleSub, AdminEmail));

        using var first = await CallbackAsync(CallbackQuery(code, start.Request.State), start.CookieHeader);
        Assert.NotNull(SessionCookie(first));

        using var replay = await CallbackAsync(CallbackQuery(code, start.Request.State), start.CookieHeader);

        Assert.Equal(LoginFailedLocation, replay.Headers.Location?.AbsoluteUri);
        AssertNoSessionCookie(replay);
    }

    private async Task AssertLoginFailedAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(LoginFailedLocation, response.Headers.Location?.AbsoluteUri);
        AssertNoSessionCookie(response);
        Assert.DoesNotContain("evil", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evil", response.Headers.ToString(), StringComparison.OrdinalIgnoreCase);

        var admin = await LoadAdminOrNullAsync();
        Assert.Null(admin?.LastLoginAt);
        Assert.Null(admin?.GoogleSub);
    }

    private async Task<AdminUser?> LoadAdminOrNullAsync()
    {
        try
        {
            return await LoadAdminAsync();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Store whose lookup is cancelled by something other than the client (e.g. a command timeout).</summary>
    private sealed class CancellingStore : IAdminLoginStore
    {
        public Task<AdminUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => throw new OperationCanceledException("test-only cancellation");

        public Task<bool> IsGoogleAccountBoundToAnotherAdminAsync(string googleSub, long adminId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
