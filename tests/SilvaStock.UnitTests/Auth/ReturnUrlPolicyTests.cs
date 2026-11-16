using SilvaStock.Application.Auth;

namespace SilvaStock.UnitTests.Auth;

public class ReturnUrlPolicyTests
{
    private static readonly Uri _site = new("https://www.silvastock.test");
    private static readonly Uri _localSite = new("http://localhost:3000");

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/")]
    [InlineData("/admin/veiculos")]
    [InlineData("/admin/veiculos/42/editar")]
    [InlineData("/admin?tab=vendidos")]
    [InlineData("/admin/veiculos?estado=Draft&page=2")]
    [InlineData("/admin/pesquisa?q=a..b")]
    [InlineData("/admin/veiculos%20novos")]
    [InlineData("/admin/100%25")]
    public void SafePathsInsideAdminAreKept(string returnUrl)
    {
        Assert.True(ReturnUrlPolicy.IsAllowed(returnUrl, _site));
        Assert.Equal(returnUrl, ReturnUrlPolicy.Sanitize(returnUrl, _site));
    }

    [Theory]
    // Vectors listed in issue #13.
    [InlineData("//evil.example.test")]
    [InlineData("/\\evil.example.test")]
    [InlineData("https://evil.example.test")]
    [InlineData("/admin/../..//evil.example.test")]
    [InlineData("/admin%2F%2Fevil")]
    [InlineData("/admin%2f%2fevil")]
    [InlineData("/adminx")]
    [InlineData("/admin/\tevil")]
    // Not under /admin, or not relative.
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("/")]
    [InlineData("/carros/42-bmw-320d-2019")]
    [InlineData("/Admin")]
    [InlineData("/admin#fragment")]
    [InlineData("/admin@evil.example.test")]
    [InlineData("/admin.evil.example.test")]
    [InlineData("http://www.silvastock.test/admin")]
    [InlineData("javascript:alert(1)")]
    [InlineData(" /admin")]
    // Control characters, raw or encoded.
    [InlineData("/admin/\r\nSet-Cookie:x=y")]
    [InlineData("/admin/\nx")]
    [InlineData("/admin/\0x")]
    [InlineData("/admin/\u007fx")]
    [InlineData("/admin/\u0085x")]
    [InlineData("/admin/%09evil")]
    [InlineData("/admin/%0d%0aSet-Cookie:x=y")]
    [InlineData("/admin/%00")]
    // Backslash, raw or encoded.
    [InlineData("/admin\\evil")]
    [InlineData("/admin/%5Cevil.example.test")]
    [InlineData("/admin%5C%5Cevil.example.test")]
    // "//" anywhere, raw or encoded (also double-encoded).
    [InlineData("/admin//evil.example.test")]
    [InlineData("/admin?next=//evil.example.test")]
    [InlineData("/admin/%2F%2Fevil.example.test")]
    [InlineData("/admin%252F%252Fevil")]
    // ".." segments, raw or encoded.
    [InlineData("/admin/..")]
    [InlineData("/admin/../carros")]
    [InlineData("/admin/%2e%2e/carros")]
    [InlineData("/admin/%2E%2E")]
    [InlineData("/admin/%252e%252e/carros")]
    [InlineData("/admin%2F..%2Fcarros")]
    public void UnsafeValuesFallBackToAdmin(string returnUrl)
    {
        Assert.False(ReturnUrlPolicy.IsAllowed(returnUrl, _site));
        Assert.Equal(ReturnUrlPolicy.DefaultReturnUrl, ReturnUrlPolicy.Sanitize(returnUrl, _site));
    }

    [Fact]
    public void NullFallsBackToAdmin()
    {
        Assert.Equal("/admin", ReturnUrlPolicy.Sanitize(null, _site));
    }

    [Fact]
    public void ValuesUpTo512CharactersAreAcceptedAndLongerOnesRejected()
    {
        var max = "/admin/" + new string('a', ReturnUrlPolicy.MaxLength - "/admin/".Length);
        var tooLong = max + "a";
        var issueVector = "/admin/" + new string('a', 600 - "/admin/".Length);

        Assert.Equal(512, max.Length);
        Assert.True(ReturnUrlPolicy.IsAllowed(max, _site));
        Assert.False(ReturnUrlPolicy.IsAllowed(tooLong, _site));
        Assert.False(ReturnUrlPolicy.IsAllowed(issueVector, _site));
    }

    [Fact]
    public void DeeplyNestedEncodingIsRejected()
    {
        // "%25252F" needs four decoding passes to reach "/"; nested encoding is never legitimate.
        Assert.False(ReturnUrlPolicy.IsAllowed("/admin/%2525252Fx", _site));
    }

    [Theory]
    [InlineData(null, "https://www.silvastock.test/admin")]
    [InlineData("/admin/veiculos?estado=Draft", "https://www.silvastock.test/admin/veiculos?estado=Draft")]
    [InlineData("//evil.example.test", "https://www.silvastock.test/admin")]
    [InlineData("https://evil.example.test/admin", "https://www.silvastock.test/admin")]
    public void RedirectUriIsAlwaysOnTheSiteOrigin(string? returnUrl, string expected)
    {
        var uri = ReturnUrlPolicy.BuildRedirectUri(returnUrl, _site);

        Assert.Equal(expected, uri.AbsoluteUri);
        Assert.Equal(_site.Scheme, uri.Scheme);
        Assert.Equal(_site.Host, uri.Host);
        Assert.Equal(_site.Port, uri.Port);
    }

    [Fact]
    public void LocalSiteKeepsSchemeHostAndPort()
    {
        var uri = ReturnUrlPolicy.BuildRedirectUri("/admin/veiculos", _localSite);

        Assert.Equal("http://localhost:3000/admin/veiculos", uri.AbsoluteUri);
    }
}
