using SilvaStock.Application.AdminUsers.Management;
using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.UnitTests.Application;

public class AdminUserManagementServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeStore _store = new();

    private AdminUserManagementService Service => new(_store, new FixedTimeProvider(_now));

    [Fact]
    public async Task AddAdminCreatesAnActiveLowerCaseAdmin()
    {
        var result = await Service.AddAdminAsync(" Maria@Example.TEST ", "Maria Silva", CancellationToken.None);

        Assert.Equal(AddAdminOutcome.Created, result.Outcome);
        Assert.Equal("maria@example.test", result.Email);
        var admin = Assert.Single(_store.Admins);
        Assert.Equal("maria@example.test", admin.Email);
        Assert.Equal("Maria Silva", admin.DisplayName);
        Assert.True(admin.IsActive);
        Assert.Equal(_now, admin.CreatedAt);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task AddAdminForAnActiveAdminChangesNothing()
    {
        _store.Admins.Add(new AdminUser("maria@example.test", "Maria", _now));

        var result = await Service.AddAdminAsync("MARIA@example.test", null, CancellationToken.None);

        Assert.Equal(AddAdminOutcome.AlreadyActive, result.Outcome);
        Assert.Equal("Maria", Assert.Single(_store.Admins).DisplayName);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task AddAdminWithTheSameNameChangesNothing()
    {
        _store.Admins.Add(new AdminUser("maria@example.test", "Maria", _now));

        var result = await Service.AddAdminAsync("maria@example.test", " Maria ", CancellationToken.None);

        Assert.Equal(AddAdminOutcome.AlreadyActive, result.Outcome);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task AddAdminWithANewNameUpdatesTheDisplayName()
    {
        _store.Admins.Add(new AdminUser("maria@example.test", "Maria", _now));

        var result = await Service.AddAdminAsync("maria@example.test", "Maria Silva", CancellationToken.None);

        Assert.Equal(AddAdminOutcome.DisplayNameUpdated, result.Outcome);
        Assert.Equal("Maria Silva", Assert.Single(_store.Admins).DisplayName);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task AddAdminReactivatesAnInactiveAdminKeepingTheSessionVersion()
    {
        var admin = new AdminUser("maria@example.test", "Maria", _now);
        admin.Deactivate();
        _store.Admins.Add(admin);

        var result = await Service.AddAdminAsync("maria@example.test", null, CancellationToken.None);

        Assert.Equal(AddAdminOutcome.Reactivated, result.Outcome);
        Assert.True(admin.IsActive);
        Assert.Equal(1, admin.SessionVersion);
        Assert.Equal("Maria", admin.DisplayName);
        Assert.Equal(1, _store.SaveCount);
    }

    [Theory]
    [InlineData("not-an-email", null, AddAdminOutcome.InvalidEmail)]
    [InlineData("maria@example.test", "   ", AddAdminOutcome.InvalidDisplayName)]
    public async Task AddAdminRejectsInvalidInputWithoutTouchingTheStore(
        string email, string? name, AddAdminOutcome expected)
    {
        var result = await Service.AddAdminAsync(email, name, CancellationToken.None);

        Assert.Equal(expected, result.Outcome);
        Assert.Empty(_store.Admins);
        Assert.Equal(0, _store.LookupCount);
    }

    [Fact]
    public async Task RemoveAdminDeactivatesAndRevokesSessions()
    {
        var admin = new AdminUser("maria@example.test", null, _now);
        _store.Admins.Add(admin);

        var result = await Service.RemoveAdminAsync("Maria@Example.test", CancellationToken.None);

        Assert.Equal(RemoveAdminOutcome.Deactivated, result.Outcome);
        Assert.False(admin.IsActive);
        Assert.Equal(1, admin.SessionVersion);
        Assert.Single(_store.Admins);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task RemoveAdminOnAnInactiveAdminRevokesSessionsAgain()
    {
        var admin = new AdminUser("maria@example.test", null, _now);
        admin.Deactivate();
        _store.Admins.Add(admin);

        var result = await Service.RemoveAdminAsync("maria@example.test", CancellationToken.None);

        Assert.Equal(RemoveAdminOutcome.AlreadyInactive, result.Outcome);
        Assert.Equal(2, admin.SessionVersion);
    }

    [Fact]
    public async Task RemoveAdminReportsNotFound()
    {
        var result = await Service.RemoveAdminAsync("nobody@example.test", CancellationToken.None);

        Assert.Equal(RemoveAdminOutcome.NotFound, result.Outcome);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task RemoveAdminRejectsAnInvalidEmail()
    {
        var result = await Service.RemoveAdminAsync("nobody", CancellationToken.None);

        Assert.Equal(RemoveAdminOutcome.InvalidEmail, result.Outcome);
        Assert.Equal(0, _store.LookupCount);
    }

    [Fact]
    public async Task ResetGoogleClearsTheSubAndRevokesSessions()
    {
        var admin = new AdminUser("maria@example.test", null, _now);
        typeof(AdminUser).GetProperty(nameof(AdminUser.GoogleSub))!.SetValue(admin, "google-sub-example-1");
        _store.Admins.Add(admin);

        var result = await Service.ResetGoogleAsync("MARIA@example.test", CancellationToken.None);

        Assert.Equal(ResetGoogleOutcome.Reset, result.Outcome);
        Assert.Null(admin.GoogleSub);
        Assert.Equal(1, admin.SessionVersion);
        Assert.True(admin.IsActive);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task ResetGoogleWithoutABoundAccountStillRevokesSessions()
    {
        var admin = new AdminUser("maria@example.test", null, _now);
        _store.Admins.Add(admin);

        var result = await Service.ResetGoogleAsync("maria@example.test", CancellationToken.None);

        Assert.Equal(ResetGoogleOutcome.NoGoogleAccountBound, result.Outcome);
        Assert.Equal(1, admin.SessionVersion);
    }

    [Fact]
    public async Task ResetGoogleReportsNotFound()
    {
        var result = await Service.ResetGoogleAsync("nobody@example.test", CancellationToken.None);

        Assert.Equal(ResetGoogleOutcome.NotFound, result.Outcome);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task ListAdminsMapsEveryAdminWithoutTheGoogleSub()
    {
        var bound = new AdminUser("b@example.test", "Bea", _now);
        typeof(AdminUser).GetProperty(nameof(AdminUser.GoogleSub))!.SetValue(bound, "google-sub-example-2");
        var inactive = new AdminUser("a@example.test", null, _now);
        inactive.Deactivate();
        _store.Admins.Add(bound);
        _store.Admins.Add(inactive);

        var list = await Service.ListAdminsAsync(CancellationToken.None);

        Assert.Equal(
            [
                new AdminUserSummary("a@example.test", null, false, null, false),
                new AdminUserSummary("b@example.test", "Bea", true, null, true),
            ],
            list);
    }

    private sealed class FakeStore : IAdminUserManagementStore
    {
        public List<AdminUser> Admins { get; } = [];

        public int SaveCount { get; private set; }

        public int LookupCount { get; private set; }

        public Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AdminUser>>(Admins.OrderBy(a => a.Email, StringComparer.Ordinal).ToList());

        public Task<AdminUser?> FindByEmailForUpdateAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            LookupCount++;
            return Task.FromResult(Admins.SingleOrDefault(a => a.Email == normalizedEmail));
        }

        public void Add(AdminUser adminUser) => Admins.Add(adminUser);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
