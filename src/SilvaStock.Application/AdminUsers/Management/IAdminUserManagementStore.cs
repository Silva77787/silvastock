using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Application.AdminUsers.Management;

/// <summary>
/// Persistence port for allowlist management. One instance is one unit of work: a row returned by
/// <see cref="FindByEmailForUpdateAsync"/> stays locked until <see cref="SaveChangesAsync"/> commits (disposing the
/// instance without saving rolls back), so a concurrent change to <c>session_version</c> (e.g. a logout) is never lost.
/// </summary>
public interface IAdminUserManagementStore
{
    /// <summary>Every administrator (active or not), ordered by email, read-only.</summary>
    Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Finds an administrator by normalised (lower-case) email and locks the row for this unit of work.</summary>
    Task<AdminUser?> FindByEmailForUpdateAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Stages a new administrator.</summary>
    void Add(AdminUser adminUser);

    /// <summary>Persists the staged changes and commits the unit of work.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
