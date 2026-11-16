using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Application.Auth;

/// <summary>Persistence port used by the Google sign-in use case (allowlist lookup in <c>admin_users</c>).</summary>
public interface IAdminLoginStore
{
    /// <summary>Finds the administrator by normalised (lower-case) email, tracked for update; null if absent.</summary>
    Task<AdminUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>True when <paramref name="googleSub"/> is already bound to an administrator other than <paramref name="adminId"/>.</summary>
    Task<bool> IsGoogleAccountBoundToAnotherAdminAsync(string googleSub, long adminId, CancellationToken cancellationToken);

    /// <summary>Persists the changes made to tracked administrators.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
