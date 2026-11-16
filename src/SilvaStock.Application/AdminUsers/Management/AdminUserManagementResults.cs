namespace SilvaStock.Application.AdminUsers.Management;

/// <summary>Outcome of <c>add-admin</c>.</summary>
public enum AddAdminOutcome
{
    /// <summary>A new active administrator was created.</summary>
    Created,

    /// <summary>An inactive administrator was put back on the allowlist.</summary>
    Reactivated,

    /// <summary>The administrator was already active; only the display name changed.</summary>
    DisplayNameUpdated,

    /// <summary>The administrator was already active with the same display name; nothing changed.</summary>
    AlreadyActive,

    /// <summary>The email is not a valid address.</summary>
    InvalidEmail,

    /// <summary>The display name is blank, too long or has control characters.</summary>
    InvalidDisplayName,
}

/// <summary>Outcome of <c>remove-admin</c>.</summary>
public enum RemoveAdminOutcome
{
    /// <summary>The administrator was deactivated and every session revoked.</summary>
    Deactivated,

    /// <summary>The administrator was already inactive; every session was revoked again.</summary>
    AlreadyInactive,

    /// <summary>No administrator has this email.</summary>
    NotFound,

    /// <summary>The email is not a valid address.</summary>
    InvalidEmail,
}

/// <summary>Outcome of <c>reset-google</c>.</summary>
public enum ResetGoogleOutcome
{
    /// <summary>The bound Google account was cleared and every session revoked.</summary>
    Reset,

    /// <summary>No Google account was bound; every session was revoked anyway.</summary>
    NoGoogleAccountBound,

    /// <summary>No administrator has this email.</summary>
    NotFound,

    /// <summary>The email is not a valid address.</summary>
    InvalidEmail,
}

/// <summary>Result of a management command: the outcome and the email it applied to (normalised when valid).</summary>
public sealed record AdminCommandResult<TOutcome>(TOutcome Outcome, string Email)
    where TOutcome : struct, Enum;

/// <summary>One row of <c>list-admins</c>. Never includes the Google <c>sub</c> itself.</summary>
public sealed record AdminUserSummary(
    string Email,
    string? DisplayName,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    bool HasGoogleAccount);
