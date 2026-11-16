using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.UnitTests.Domain;

/// <summary>Allowlist management members of <see cref="AdminUser"/> (F2-05).</summary>
public class AdminUserManagementTests
{
    private static readonly DateTimeOffset _createdAt = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DeactivateMakesTheAdminInactiveAndRevokesSessions()
    {
        var user = NewAdmin();

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.Equal(1, user.SessionVersion);
    }

    [Fact]
    public void DeactivateTwiceRevokesSessionsEachTime()
    {
        var user = NewAdmin();

        user.Deactivate();
        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.Equal(2, user.SessionVersion);
    }

    [Fact]
    public void ReactivateMakesTheAdminActiveWithoutResettingTheSessionVersion()
    {
        var user = NewAdmin();
        user.Deactivate();

        user.Reactivate();

        Assert.True(user.IsActive);
        Assert.Equal(1, user.SessionVersion);
    }

    [Fact]
    public void RevokeSessionsIncrementsTheSessionVersion()
    {
        var user = NewAdmin();

        user.RevokeSessions();
        user.RevokeSessions();

        Assert.Equal(2, user.SessionVersion);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void ResetGoogleAccountClearsTheSubAndRevokesSessions()
    {
        var user = NewAdmin();
        SetGoogleSub(user, "google-sub-example-1");

        user.ResetGoogleAccount();

        Assert.Null(user.GoogleSub);
        Assert.Equal(1, user.SessionVersion);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void ResetGoogleAccountWithoutABoundAccountStillRevokesSessions()
    {
        var user = NewAdmin();

        user.ResetGoogleAccount();

        Assert.Null(user.GoogleSub);
        Assert.Equal(1, user.SessionVersion);
    }

    [Theory]
    [InlineData("  Novo Nome  ", "Novo Nome")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void ChangeDisplayNameTrimsAndTurnsBlankIntoNull(string? displayName, string? expected)
    {
        var user = new AdminUser("admin@example.test", "Antigo", _createdAt);

        user.ChangeDisplayName(displayName);

        Assert.Equal(expected, user.DisplayName);
    }

    private static AdminUser NewAdmin() => new("admin@example.test", "Admin Exemplo", _createdAt);

    // GoogleSub is bound by the login flow (F2-03); tests set it directly.
    private static void SetGoogleSub(AdminUser user, string sub)
        => typeof(AdminUser).GetProperty(nameof(AdminUser.GoogleSub))!.SetValue(user, sub);
}
