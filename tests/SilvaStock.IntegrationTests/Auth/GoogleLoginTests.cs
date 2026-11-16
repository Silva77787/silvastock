using System.Globalization;
using System.Net;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// <c>GET /api/auth/login/google</c> and <c>GET /api/auth/callback/google</c> (ADR-0004 Login/Session, issue #13):
/// success path and the allowlist refusals (<c>not_allowed</c>).
/// </summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class GoogleLoginTests(PostgresFixture postgres) : GoogleLoginTestBase(postgres)
{
    [Fact]
    public async Task LoginRedirectsToIssuerWithCodeFlowPkceAndQueryResponseMode()
    {
        var start = await StartLoginAsync("/admin/veiculos");
        var query = start.Request.Query;

        Assert.Equal(SilvaStockApiFactory.TestGoogleClientId, query["client_id"]);
        Assert.Equal("code", query["response_type"]);
        // response_mode=query is the default for response_type=code, so the handler omits it (GET callback).
        Assert.True(!query.ContainsKey("response_mode") || query["response_mode"] == "query");
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.False(string.IsNullOrEmpty(start.Request.CodeChallenge));
        Assert.False(string.IsNullOrEmpty(start.Request.Nonce));
        Assert.False(string.IsNullOrEmpty(start.Request.State));
        Assert.Equal(new HashSet<string> { "openid", "email", "profile" }, query["scope"].Split(' ').ToHashSet());
        Assert.Equal("https://localhost" + CallbackPath, start.Request.RedirectUri);

        // The return URL travels only inside the protected state, never in plain text.
        Assert.DoesNotContain("veiculos", start.Request.Location.AbsoluteUri, StringComparison.Ordinal);

        // Correlation and nonce cookies: always Secure, HttpOnly, SameSite=Lax, short-lived. No session yet.
        var cookies = SetCookies(start.Response);
        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, c =>
        {
            Assert.True(c.Secure);
            Assert.True(c.HttpOnly);
            Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Lax, c.SameSite);
            Assert.NotNull(c.Expires);
            Assert.InRange(c.Expires!.Value, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(11));
        });
        AssertNoSessionCookie(start.Response);
    }

    [Fact]
    public async Task VerifiedActiveAdminGetsSessionCookieAndIsRedirectedToReturnUrl()
    {
        var adminId = await SeedAdminAsync();
        var before = DateTimeOffset.UtcNow;

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail), "/admin/veiculos?estado=Draft");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(Site + "/admin/veiculos?estado=Draft", response.Headers.Location?.AbsoluteUri);

        var cookie = SessionCookie(response);
        Assert.NotNull(cookie);
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Lax, cookie.SameSite);
        Assert.Equal("/", cookie.Path.Value);
        Assert.True(cookie.Domain.Value is null, "The session cookie must be host-only (no Domain).");
        Assert.NotNull(cookie.Expires); // persistent
        Assert.InRange(cookie.Expires!.Value, before.AddDays(30).AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30).AddMinutes(5));

        var admin = await LoadAdminAsync();
        Assert.Equal(GoogleSub, admin.GoogleSub);
        Assert.NotNull(admin.LastLoginAt);
        Assert.InRange(admin.LastLoginAt!.Value, before.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5));

        var claims = await ReadSessionClaimsAsync(cookie);
        Assert.Equal(adminId.ToString(CultureInfo.InvariantCulture), claims["admin_id"]);
        Assert.Equal(AdminEmail, claims["email"]);
        Assert.Equal("0", claims["session_version"]);
        Assert.Equal("Admin Exemplo", claims["name"]);
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims["issued_at"], CultureInfo.InvariantCulture));
        Assert.InRange(issuedAt, before.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5));

        // Only the expected claims: no Google tokens or other id_token claims in the session.
        Assert.Equal(
            new HashSet<string> { "admin_id", "email", "session_version", "name", "issued_at" },
            claims.Keys.ToHashSet());
    }

    [Fact]
    public async Task LoginWithoutReturnUrlRedirectsToAdmin()
    {
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail));

        Assert.Equal(Site + "/admin", response.Headers.Location?.AbsoluteUri);
        Assert.NotNull(SessionCookie(response));
    }

    [Fact]
    public async Task MixedCaseEmailFromGoogleSucceeds()
    {
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, "Admin@Example.TEST"));

        Assert.Equal(Site + "/admin", response.Headers.Location?.AbsoluteUri);
        Assert.NotNull(SessionCookie(response));
    }

    [Fact]
    public async Task AdminAlreadyBoundToTheSameGoogleAccountSucceeds()
    {
        await SeedAdminAsync(googleSub: GoogleSub);

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail));

        Assert.Equal(Site + "/admin", response.Headers.Location?.AbsoluteUri);
        Assert.NotNull(SessionCookie(response));
        Assert.Equal(GoogleSub, (await LoadAdminAsync()).GoogleSub);
    }

    public static TheoryData<string, object?> UnverifiedEmails => new()
    {
        { "false (boolean)", false },
        { "missing", null },
        { "\"false\" (string)", "false" },
        { "garbage", "yes" },
    };

    [Theory]
    [MemberData(nameof(UnverifiedEmails))]
    public async Task UnverifiedOrMissingEmailVerifiedIsNotAllowed(string description, object? emailVerified)
    {
        Assert.NotNull(description);
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail) { EmailVerified = emailVerified });

        await AssertNotAllowedAsync(response);
    }

    [Fact]
    public async Task UnknownEmailIsNotAllowed()
    {
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, "someone@example.test"));

        await AssertNotAllowedAsync(response);
    }

    [Fact]
    public async Task InactiveAdminIsNotAllowed()
    {
        await SeedAdminAsync(isActive: false);

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail));

        await AssertNotAllowedAsync(response);
    }

    [Fact]
    public async Task DifferentGoogleAccountThanTheBoundOneIsNotAllowed()
    {
        await SeedAdminAsync(googleSub: GoogleSub);

        using var response = await LoginAsync(new FakeIdToken("google-sub-999888", AdminEmail));

        await AssertNotAllowedAsync(response, expectedGoogleSub: GoogleSub);
    }

    [Fact]
    public async Task GoogleAccountBoundToAnotherAdminIsNotAllowed()
    {
        await SeedAdminAsync("other@example.test", googleSub: GoogleSub);
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail));

        await AssertNotAllowedAsync(response);
    }

    [Fact]
    public async Task RefusedLoginNeverLogsTheFullEmailOrTokens()
    {
        await SeedAdminAsync();

        using var refused = await LoginAsync(new FakeIdToken(GoogleSub, "intruder@example.test"));
        using var accepted = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail));
        var sessionCookie = SessionCookie(accepted);
        Assert.NotNull(sessionCookie);

        var logs = Factory.Logs.ToList();
        Assert.Contains(logs, l => l.Contains("not_allowed (UnknownEmail), email domain example.test", StringComparison.Ordinal));
        Assert.Contains(logs, l => l.Contains("Google login succeeded for admin", StringComparison.Ordinal));
        Assert.All(logs, l =>
        {
            Assert.DoesNotContain("intruder@example.test", l, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(AdminEmail, l, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fake-access-token", l, StringComparison.Ordinal);
            Assert.DoesNotContain(sessionCookie.Value.Value!, l, StringComparison.Ordinal);
            Assert.DoesNotContain(SilvaStockApiFactory.TestGoogleClientSecret, l, StringComparison.Ordinal);
        });
    }

    private async Task AssertNotAllowedAsync(HttpResponseMessage response, string? expectedGoogleSub = null)
    {
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(NotAllowedLocation, response.Headers.Location?.AbsoluteUri);
        AssertNoSessionCookie(response);

        // The token itself was valid: the refusal comes from the allowlist rules, after the code was redeemed.
        Assert.Equal(1, Factory.Issuer.RedeemedCodes);

        var admin = await LoadAdminAsync();
        Assert.Null(admin.LastLoginAt);
        Assert.Equal(expectedGoogleSub, admin.GoogleSub);
    }
}
