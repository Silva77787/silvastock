using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.IntegrationTests.Infrastructure;
using SilvaStock.IntegrationTests.Persistence;

namespace SilvaStock.IntegrationTests.Auth;

/// <summary>
/// Drives the browser side of the Google login against <see cref="GoogleLoginApiFactory"/>: start the login, let the
/// fake issuer issue a code, call the callback with the correlation/nonce cookies, and inspect the result.
/// Every test starts from a migrated, empty database. Fictitious data only (<c>example.test</c>).
/// </summary>
public abstract class GoogleLoginTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    public const string AdminEmail = "admin@example.test";
    public const string GoogleSub = "google-sub-000111";
    public const string SessionCookieName = "__Host-silvastock-auth";
    public const string LoginPath = "/api/auth/login/google";
    public const string CallbackPath = "/api/auth/callback/google";

    protected static readonly string Site = SilvaStockApiFactory.TestSiteOrigin;
    protected static readonly string NotAllowedLocation = Site + "/admin/login?error=not_allowed";
    protected static readonly string LoginFailedLocation = Site + "/admin/login?error=login_failed";

    protected PostgresFixture Postgres { get; } = postgres;

    protected GoogleLoginApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await MigratedDatabase.PrepareAsync(Postgres);
        Factory = new GoogleLoginApiFactory(Postgres.ConnectionString);
        Client = Factory.CreateBrowserClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>Adds an administrator; optionally already bound to a Google account and/or inactive.</summary>
    protected async Task<long> SeedAdminAsync(string email = AdminEmail, string? googleSub = null, bool isActive = true)
    {
        await using var db = MigratedDatabase.CreateContext(Postgres);
        var admin = new AdminUser(email, "Admin Exemplo", DateTimeOffset.UtcNow);
        if (googleSub is not null)
        {
            admin.BindGoogleAccount(googleSub);
        }

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        if (!isActive)
        {
            // Deactivation belongs to the CLI task (F2-05); a direct update is enough here.
            await db.AdminUsers.Where(u => u.Id == admin.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
        }

        return admin.Id;
    }

    protected async Task<AdminUser> LoadAdminAsync(string email = AdminEmail)
    {
        await using var db = MigratedDatabase.CreateContext(Postgres);
        return await db.AdminUsers.AsNoTracking().SingleAsync(u => u.Email == email);
    }

    /// <summary>Starts the login; returns the 302 response, the parsed authorization request and the cookies to send back.</summary>
    protected async Task<LoginStart> StartLoginAsync(string? returnUrl = null)
    {
        var path = returnUrl is null ? LoginPath : $"{LoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}";
        var response = await Client.GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = Assert.IsType<Uri>(response.Headers.Location);
        Assert.StartsWith(FakeOidcIssuer.AuthorizationEndpoint + "?", location.AbsoluteUri, StringComparison.Ordinal);

        var cookieHeader = string.Join("; ", SetCookies(response).Select(c => $"{c.Name}={c.Value}"));
        return new LoginStart(response, AuthorizationRequest.Parse(location), cookieHeader);
    }

    /// <summary>Calls the callback as the browser would after Google redirects back.</summary>
    protected async Task<HttpResponseMessage> CallbackAsync(string query, string? cookieHeader)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{CallbackPath}?{query}", UriKind.Relative));
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        return await Client.SendAsync(request);
    }

    /// <summary>Full login: start, issue a code for <paramref name="token"/>, then the callback.</summary>
    protected async Task<HttpResponseMessage> LoginAsync(FakeIdToken token, string? returnUrl = null)
    {
        var start = await StartLoginAsync(returnUrl);
        var code = Factory.Issuer.IssueCode(start.Request, token);
        return await CallbackAsync(CallbackQuery(code, start.Request.State), start.CookieHeader);
    }

    protected static string CallbackQuery(string code, string state)
        => $"code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}";

    protected static IList<SetCookieHeaderValue> SetCookies(HttpResponseMessage response)
        => response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
            ? SetCookieHeaderValue.ParseList([.. values])
            : [];

    /// <summary>The session cookie set by <paramref name="response"/> with a value, or null.</summary>
    protected static SetCookieHeaderValue? SessionCookie(HttpResponseMessage response)
        => SetCookies(response).SingleOrDefault(c =>
            c.Name.Equals(SessionCookieName, StringComparison.Ordinal) && c.Value.Length > 0);

    protected static void AssertNoSessionCookie(HttpResponseMessage response)
        => Assert.DoesNotContain(
            SetCookies(response),
            c => c.Name.Value!.StartsWith(SessionCookieName, StringComparison.Ordinal) && c.Value.Length > 0);

    /// <summary>Reads the session claims through the test probe using only the session cookie.</summary>
    protected async Task<Dictionary<string, string>> ReadSessionClaimsAsync(SetCookieHeaderValue sessionCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(GoogleLoginApiFactory.SessionProbePath, UriKind.Relative));
        request.Headers.Add("Cookie", $"{sessionCookie.Name}={sessionCookie.Value}");
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var claims = JsonSerializer.Deserialize<Dictionary<string, string>>(await response.Content.ReadAsStringAsync());
        return Assert.IsType<Dictionary<string, string>>(claims);
    }

    protected sealed record LoginStart(HttpResponseMessage Response, AuthorizationRequest Request, string CookieHeader);
}
