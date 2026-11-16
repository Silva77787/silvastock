using System.Reflection;
using SilvaStock.Application.Auth;
using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.UnitTests.Auth;

public class GoogleSignInServiceTests
{
    private const string _email = "admin@example.test";
    private const string _sub = "google-sub-000111";

    private static readonly DateTimeOffset _now = new(2026, 11, 16, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeStore _store = new();
    private readonly GoogleSignInService _service;

    public GoogleSignInServiceTests()
    {
        _service = new GoogleSignInService(_store, new FixedTimeProvider(_now));
    }

    [Fact]
    public async Task FirstLoginBindsTheGoogleAccountRecordsTheLoginAndReturnsTheSession()
    {
        var admin = _store.Add(_email, id: 7);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, _email, "true", " Admin Exemplo "), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Failure);
        Assert.Equal(new AdminSession(7, _email, 0, "Admin Exemplo", _now), result.Session);
        Assert.Equal(_sub, admin.GoogleSub);
        Assert.Equal(_now, admin.LastLoginAt);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task LaterLoginWithTheBoundAccountSucceeds()
    {
        var admin = _store.Add(_email, id: 7, googleSub: _sub);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, _email, "true", null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Session!.GoogleName);
        Assert.Equal(_now, admin.LastLoginAt);
    }

    [Theory]
    [InlineData("ADMIN@EXAMPLE.TEST")]
    [InlineData("Admin@Example.test")]
    public async Task EmailIsMatchedCaseInsensitively(string email)
    {
        _store.Add(_email, id: 7);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, email, "true", null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(_email, result.Session!.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("1")]
    [InlineData("yes")]
    public async Task EmailNotVerifiedIsRefused(string? emailVerified)
    {
        _store.Add(_email, id: 7);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, _email, emailVerified, null), CancellationToken.None);

        AssertRefused(result, GoogleSignInFailure.EmailNotVerified);
    }

    [Theory]
    [InlineData(null, _email)]
    [InlineData(" ", _email)]
    [InlineData(_sub, null)]
    [InlineData(_sub, "")]
    public async Task MissingSubjectOrEmailIsRefused(string? sub, string? email)
    {
        _store.Add(_email, id: 7);

        var result = await _service.SignInAsync(new GoogleIdentity(sub, email, "true", null), CancellationToken.None);

        AssertRefused(result, GoogleSignInFailure.MissingClaims);
    }

    [Fact]
    public async Task UnknownEmailIsRefused()
    {
        _store.Add(_email, id: 7);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, "other@example.test", "true", null), CancellationToken.None);

        AssertRefused(result, GoogleSignInFailure.UnknownEmail);
    }

    [Fact]
    public async Task InactiveAdminIsRefused()
    {
        _store.Add(_email, id: 7, isActive: false);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, _email, "true", null), CancellationToken.None);

        AssertRefused(result, GoogleSignInFailure.InactiveAdmin);
    }

    [Fact]
    public async Task DifferentGoogleAccountThanTheBoundOneIsRefused()
    {
        var admin = _store.Add(_email, id: 7, googleSub: _sub);

        var result = await _service.SignInAsync(new GoogleIdentity("google-sub-999", _email, "true", null), CancellationToken.None);

        AssertRefused(result, GoogleSignInFailure.GoogleAccountMismatch);
        Assert.Equal(_sub, admin.GoogleSub);
    }

    [Fact]
    public async Task GoogleAccountBoundToAnotherAdminIsRefused()
    {
        _store.Add("other@example.test", id: 3, googleSub: _sub);
        var admin = _store.Add(_email, id: 7);

        var result = await _service.SignInAsync(new GoogleIdentity(_sub, _email, "true", null), CancellationToken.None);

        AssertRefused(result, GoogleSignInFailure.GoogleAccountBoundToAnotherAdmin);
        Assert.Null(admin.GoogleSub);
    }

    private void AssertRefused(GoogleSignInResult result, GoogleSignInFailure expected)
    {
        Assert.False(result.Succeeded);
        Assert.Null(result.Session);
        Assert.Equal(expected, result.Failure);
        Assert.Equal(0, _store.SaveCount);
        Assert.All(_store.Admins, a => Assert.Null(a.LastLoginAt));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : IAdminLoginStore
    {
        public List<AdminUser> Admins { get; } = [];

        public int SaveCount { get; private set; }

        public AdminUser Add(string email, long id, string? googleSub = null, bool isActive = true)
        {
            var admin = new AdminUser(email, displayName: null, _now.AddDays(-10));
            SetPrivate(admin, nameof(AdminUser.Id), id);
            SetPrivate(admin, nameof(AdminUser.IsActive), isActive);
            if (googleSub is not null)
            {
                admin.BindGoogleAccount(googleSub);
            }

            Admins.Add(admin);
            return admin;
        }

        public Task<AdminUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult(Admins.SingleOrDefault(a => a.Email == normalizedEmail));

        public Task<bool> IsGoogleAccountBoundToAnotherAdminAsync(string googleSub, long adminId, CancellationToken cancellationToken)
            => Task.FromResult(Admins.Any(a => a.GoogleSub == googleSub && a.Id != adminId));

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        // Id is assigned by the database and IsActive changes belong to F2-05; tests set them directly.
        private static void SetPrivate(AdminUser admin, string property, object value)
            => typeof(AdminUser).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!.SetValue(admin, value);
    }
}
