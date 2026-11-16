using System.Net;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// Open-redirect vectors of issue #13: whatever <c>returnUrl</c> is sent to <c>GET /api/auth/login/google</c>, a
/// successful login ends at <c>{site}/admin</c> unless the value is a safe path inside <c>/admin</c>.
/// </summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class LoginReturnUrlTests(PostgresFixture postgres) : GoogleLoginTestBase(postgres)
{
    public static TheoryData<string> OpenRedirectVectors => new()
    {
        "//evil.example.test",
        "/\\evil.example.test",
        "https://evil.example.test",
        "/admin/../..//evil.example.test",
        "/admin%2F%2Fevil",
        "/adminx",
        "/admin/\tevil",
        "/admin/" + new string('a', 593),
        "/admin//evil.example.test",
        "/admin/%2e%2e/%2e%2e/evil",
        "/admin@evil.example.test",
        "javascript:alert(1)",
        "",
    };

    [Theory]
    [MemberData(nameof(OpenRedirectVectors))]
    public async Task UnsafeReturnUrlEndsAtSiteAdmin(string returnUrl)
    {
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail), returnUrl);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(Site + "/admin", response.Headers.Location?.AbsoluteUri);
        Assert.NotNull(SessionCookie(response));
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/")]
    [InlineData("/admin/veiculos/42")]
    [InlineData("/admin?tab=vendidos")]
    public async Task SafeReturnUrlIsKept(string returnUrl)
    {
        await SeedAdminAsync();

        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail), returnUrl);

        Assert.Equal(Site + returnUrl, response.Headers.Location?.AbsoluteUri);
    }

    [Fact]
    public async Task ReturnUrlIsIgnoredWhenTheLoginIsRefused()
    {
        using var response = await LoginAsync(new FakeIdToken(GoogleSub, AdminEmail), "/admin/veiculos");

        Assert.Equal(NotAllowedLocation, response.Headers.Location?.AbsoluteUri);
    }
}
