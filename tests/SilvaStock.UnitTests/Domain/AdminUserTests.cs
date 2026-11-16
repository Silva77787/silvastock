using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.UnitTests.Domain;

public class AdminUserTests
{
    private static readonly DateTimeOffset _createdAt = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("admin@example.test", "admin@example.test")]
    [InlineData("Admin@Example.TEST", "admin@example.test")]
    [InlineData("  Admin@example.test  ", "admin@example.test")]
    public void EmailIsStoredTrimmedAndLowerCase(string email, string expected)
    {
        var user = new AdminUser(email, displayName: null, _createdAt);

        Assert.Equal(expected, user.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankEmailIsRejected(string email)
    {
        Assert.Throws<ArgumentException>(() => new AdminUser(email, displayName: null, _createdAt));
    }

    [Fact]
    public void NewAdminIsActiveWithNoLoginNoGoogleAccountAndSessionVersionZero()
    {
        var user = new AdminUser("admin@example.test", "Admin Exemplo", _createdAt);

        Assert.True(user.IsActive);
        Assert.Null(user.LastLoginAt);
        Assert.Null(user.GoogleSub);
        Assert.Equal(0, user.SessionVersion);
        Assert.Equal("Admin Exemplo", user.DisplayName);
        Assert.Equal(_createdAt, user.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankDisplayNameIsStoredAsNull(string? displayName)
    {
        var user = new AdminUser("admin@example.test", displayName, _createdAt);

        Assert.Null(user.DisplayName);
    }

    [Fact]
    public void CreatedAtIsStoredInUtc()
    {
        var lisbonSummer = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.FromHours(1));

        var user = new AdminUser("admin@example.test", displayName: null, lisbonSummer);

        Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
        Assert.Equal(lisbonSummer.UtcDateTime, user.CreatedAt.UtcDateTime);
    }
}
