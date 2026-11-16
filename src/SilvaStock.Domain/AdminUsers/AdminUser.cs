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

    /// <summary>True when a Google account is bound and its <c>sub</c> is exactly <paramref name="googleSub"/>.</summary>
    public bool IsBoundTo(string googleSub)
        => GoogleSub is not null && string.Equals(GoogleSub, googleSub, StringComparison.Ordinal);

    /// <summary>
    /// Binds the Google account (<c>sub</c>) on the first login (ADR-0004). Binding the same account again is a
    /// no-op; a different account can only be bound after the binding is reset (CLI <c>reset-google</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">Another Google account is already bound.</exception>
    public void BindGoogleAccount(string googleSub)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleSub);

        if (GoogleSub is null)
        {
            GoogleSub = googleSub;
            return;
        }

        if (!IsBoundTo(googleSub))
        {
            throw new InvalidOperationException("A different Google account is already bound to this administrator.");
        }
    }

    /// <summary>Records a successful login (stored in UTC).</summary>
    public void RecordLogin(DateTimeOffset at) => LastLoginAt = at.ToUniversalTime();

    /// <summary>Normalises an email for storage and lookup: trimmed, lower case (invariant culture).</summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }
}
