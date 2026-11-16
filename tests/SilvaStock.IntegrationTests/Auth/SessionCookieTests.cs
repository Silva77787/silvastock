using System.Net;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// Session scheme behaviour that F2-03 owns (ADR-0004 Session): API challenges/forbids never redirect, and the OIDC
/// handler's built-in sign-out endpoints are disabled.
/// </summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class SessionCookieTests(PostgresFixture postgres) : GoogleLoginTestBase(postgres)
{
    [Fact]
    public async Task ChallengeWithoutSessionIs401ProblemDetailsWithoutRedirect()
    {
        using var response = await Client.GetAsync(new Uri(GoogleLoginApiFactory.SessionProbePath, UriKind.Relative));

        await ProblemDetailsAssert.IsProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task ChallengeWithInvalidSessionCookieIs401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(GoogleLoginApiFactory.SessionProbePath, UriKind.Relative));
        request.Headers.Add("Cookie", $"{SessionCookieName}=forged-value");

        using var response = await Client.SendAsync(request);

        await ProblemDetailsAssert.IsProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task ForbidIs403ProblemDetailsWithoutRedirect()
    {
        using var response = await Client.GetAsync(new Uri(GoogleLoginApiFactory.ForbidProbePath, UriKind.Relative));

        await ProblemDetailsAssert.IsProblemDetailsAsync(response, HttpStatusCode.Forbidden);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("/signout-oidc")]
    [InlineData("/signout-callback-oidc")]
    public async Task OidcSignOutEndpointsAreDisabled(string path)
    {
        await SeedAdminAsync();
        using var login = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail));
        var sessionCookie = SessionCookie(login);
        Assert.NotNull(sessionCookie);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Add("Cookie", $"{sessionCookie.Name}={sessionCookie.Value}");
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Empty(SetCookies(response));

        // The session is still usable.
        var claims = await ReadSessionClaimsAsync(sessionCookie);
        Assert.Equal(AdminEmail, claims["email"]);
    }
}
