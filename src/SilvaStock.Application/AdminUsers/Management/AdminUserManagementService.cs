using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Application.AdminUsers.Management;

/// <summary>
/// Allowlist management use cases behind the CLI (DECISIONS section 5, ADR-0004 "Management"). Rows are never
/// deleted; removal and Google reset always increment <c>session_version</c> so existing sessions end.
/// </summary>
public sealed class AdminUserManagementService(IAdminUserManagementStore store, TimeProvider timeProvider)
{
    /// <summary>
    /// Creates the administrator, or reactivates an inactive one; idempotent for an active one. When
    /// <paramref name="displayName"/> is given it replaces the stored display name; when null it is left as is.
    /// </summary>
    public async Task<AdminCommandResult<AddAdminOutcome>> AddAdminAsync(
        string email, string? displayName, CancellationToken cancellationToken)
    {
        if (!AdminEmail.TryNormalize(email, out var normalized))
        {
            return new(AddAdminOutcome.InvalidEmail, email);
        }

        if (displayName is not null && !AdminDisplayName.IsValid(displayName))
        {
            return new(AddAdminOutcome.InvalidDisplayName, normalized);
        }

        var existing = await store.FindByEmailForUpdateAsync(normalized, cancellationToken);
        if (existing is null)
        {
            try
            {
                store.Add(new AdminUser(normalized, displayName, timeProvider.GetUtcNow()));
                await store.SaveChangesAsync(cancellationToken);
                return new(AddAdminOutcome.Created, normalized);
            }
            catch (AdminEmailAlreadyExistsException)
            {
                // A concurrent add-admin inserted the same email first: continue as for an existing admin.
                existing = await store.FindByEmailForUpdateAsync(normalized, cancellationToken)
                    ?? throw new InvalidOperationException("The administrator inserted concurrently was not found.");
            }
        }

        return await UpdateExistingAsync(existing, normalized, displayName, cancellationToken);
    }

    private async Task<AdminCommandResult<AddAdminOutcome>> UpdateExistingAsync(
        AdminUser existing, string normalized, string? displayName, CancellationToken cancellationToken)
    {
        var outcome = AddAdminOutcome.AlreadyActive;
        if (!existing.IsActive)
        {
            existing.Reactivate();
            outcome = AddAdminOutcome.Reactivated;
        }

        if (displayName is not null
            && !string.Equals(existing.DisplayName, displayName.Trim(), StringComparison.Ordinal))
        {
            existing.ChangeDisplayName(displayName);
            if (outcome == AddAdminOutcome.AlreadyActive)
            {
                outcome = AddAdminOutcome.DisplayNameUpdated;
            }
        }

        if (outcome != AddAdminOutcome.AlreadyActive)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return new(outcome, normalized);
    }

    /// <summary>Deactivates the administrator (the row is kept) and revokes every session.</summary>
    public async Task<AdminCommandResult<RemoveAdminOutcome>> RemoveAdminAsync(
        string email, CancellationToken cancellationToken)
    {
        if (!AdminEmail.TryNormalize(email, out var normalized))
        {
            return new(RemoveAdminOutcome.InvalidEmail, email);
        }

        var existing = await store.FindByEmailForUpdateAsync(normalized, cancellationToken);
        if (existing is null)
        {
            return new(RemoveAdminOutcome.NotFound, normalized);
        }

        var wasActive = existing.IsActive;

        // Always revoke, even when already inactive: also covers a deactivation made without a version change.
        existing.Deactivate();
        await store.SaveChangesAsync(cancellationToken);

        return new(wasActive ? RemoveAdminOutcome.Deactivated : RemoveAdminOutcome.AlreadyInactive, normalized);
    }

    /// <summary>Clears the bound Google account and revokes every session.</summary>
    public async Task<AdminCommandResult<ResetGoogleOutcome>> ResetGoogleAsync(
        string email, CancellationToken cancellationToken)
    {
        if (!AdminEmail.TryNormalize(email, out var normalized))
        {
            return new(ResetGoogleOutcome.InvalidEmail, email);
        }

        var existing = await store.FindByEmailForUpdateAsync(normalized, cancellationToken);
        if (existing is null)
        {
            return new(ResetGoogleOutcome.NotFound, normalized);
        }

        var wasBound = existing.GoogleSub is not null;
        existing.ResetGoogleAccount();
        await store.SaveChangesAsync(cancellationToken);

        return new(wasBound ? ResetGoogleOutcome.Reset : ResetGoogleOutcome.NoGoogleAccountBound, normalized);
    }

    /// <summary>Every administrator, ordered by email.</summary>
    public async Task<IReadOnlyList<AdminUserSummary>> ListAdminsAsync(CancellationToken cancellationToken)
    {
        var admins = await store.ListAsync(cancellationToken);
        return admins
            .Select(a => new AdminUserSummary(a.Email, a.DisplayName, a.IsActive, a.LastLoginAt, a.GoogleSub is not null))
            .ToList();
    }
}
