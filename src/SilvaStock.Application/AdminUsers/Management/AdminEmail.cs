using System.Net.Mail;
using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Application.AdminUsers.Management;

/// <summary>Validation of the email typed by an operator when managing the allowlist.</summary>
public static class AdminEmail
{
    /// <summary>
    /// Trims and lower-cases <paramref name="input"/> (<c>ToLowerInvariant</c>, ADR-0004) and checks it is a plain
    /// address (<c>local@domain.tld</c>, no display name, no spaces or control characters, at most
    /// <see cref="AdminUser.EmailMaxLength"/> characters).
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = AdminUser.NormalizeEmail(input);
        if (candidate.Length > AdminUser.EmailMaxLength
            || candidate.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))
            || candidate.Count(c => c == '@') != 1)
        {
            return false;
        }

        if (!MailAddress.TryCreate(candidate, out var address)
            || !string.Equals(address.Address, candidate, StringComparison.Ordinal)
            || address.DisplayName.Length != 0)
        {
            return false;
        }

        var domain = address.Host;
        if (!domain.Contains('.', StringComparison.Ordinal)
            || domain.StartsWith('.')
            || domain.EndsWith('.')
            || domain.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }
}
