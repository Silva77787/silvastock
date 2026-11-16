using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Application.AdminUsers.Management;

/// <summary>Validation of the optional display name given with <c>add-admin --name</c>.</summary>
public static class AdminDisplayName
{
    /// <summary>
    /// A display name is valid when it is not blank, has no control characters and, once trimmed, has at most
    /// <see cref="AdminUser.DisplayNameMaxLength"/> characters.
    /// </summary>
    public static bool IsValid(string? displayName)
        => !string.IsNullOrWhiteSpace(displayName)
            && !displayName.Any(char.IsControl)
            && displayName.Trim().Length <= AdminUser.DisplayNameMaxLength;
}
