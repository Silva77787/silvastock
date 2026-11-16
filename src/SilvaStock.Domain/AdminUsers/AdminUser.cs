namespace SilvaStock.Domain.AdminUsers;

/// <summary>
/// An administrator allowed to sign in to the back office (allowlist, ADR-0004).
/// Sign-in, revocation and Google account binding rules are implemented by the auth tasks (F2-03..F2-05).
/// </summary>
public sealed class AdminUser
{
    /// <summary>Maximum email length (RFC 5321 path limit).</summary>
    public const int EmailMaxLength = 254;

    /// <summary>Maximum display name length.</summary>
    public const int DisplayNameMaxLength = 100;

    /// <summary>Creates an active administrator. The email is stored trimmed and in lower case.</summary>
    public AdminUser(string email, string? displayName, DateTimeOffset createdAt)
    {
        Email = NormalizeEmail(email);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        CreatedAt = createdAt.ToUniversalTime();
        IsActive = true;
    }

    public long Id { get; private set; }

    /// <summary>Lower-case email; unique.</summary>
    public string Email { get; private set; }

    public string? DisplayName { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>Incremented to revoke every session of this administrator (ADR-0004).</summary>
    public int SessionVersion { get; private set; }

    /// <summary>Google <c>sub</c> bound on first login; unique when set.</summary>
    public string? GoogleSub { get; private set; }

    // ---- Allowlist management (CLI, F2-05) ----

    /// <summary>
    /// Removes the administrator from the allowlist (the row is kept) and revokes every session,
    /// so an existing cookie is rejected on its next request (ADR-0004).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        RevokeSessions();
    }

    /// <summary>Puts a deactivated administrator back on the allowlist. Sessions revoked earlier stay revoked.</summary>
    public void Reactivate() => IsActive = true;

    /// <summary>Signs the administrator out on every device by incrementing <see cref="SessionVersion"/>.</summary>
    public void RevokeSessions() => SessionVersion = checked(SessionVersion + 1);

    /// <summary>
    /// Unbinds the Google account so a different one can be bound on the next login, and revokes every
    /// session (including those of the old account).
    /// </summary>
    public void ResetGoogleAccount()
    {
        GoogleSub = null;
        RevokeSessions();
    }

    /// <summary>Sets the display name, normalised like in the constructor (trimmed; blank becomes null).</summary>
    public void ChangeDisplayName(string? displayName)
        => DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

    /// <summary>Normalises an email for storage and lookup: trimmed, lower case (invariant culture).</summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }
}
