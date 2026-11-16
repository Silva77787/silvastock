using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Application.Auth;

/// <summary>
/// Allowlist rules applied after the Google <c>id_token</c> has been validated (ADR-0004, Login, steps 1-4):
/// verified email, active administrator, Google account binding, then <c>last_login_at</c> is recorded.
/// </summary>
public sealed class GoogleSignInService(IAdminLoginStore store, TimeProvider timeProvider)
{
    public async Task<GoogleSignInResult> SignInAsync(GoogleIdentity identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (string.IsNullOrWhiteSpace(identity.Subject) || string.IsNullOrWhiteSpace(identity.Email))
        {
            return GoogleSignInResult.Refused(GoogleSignInFailure.MissingClaims);
        }

        // 1. email_verified must be present and true.
        if (!IsTrue(identity.EmailVerified))
        {
            return GoogleSignInResult.Refused(GoogleSignInFailure.EmailNotVerified);
        }

        // 2. Active row in admin_users, looked up by the lower-case email.
        var admin = await store.FindByEmailAsync(AdminUser.NormalizeEmail(identity.Email), cancellationToken);
        if (admin is null)
        {
            return GoogleSignInResult.Refused(GoogleSignInFailure.UnknownEmail);
        }

        if (!admin.IsActive)
        {
            return GoogleSignInResult.Refused(GoogleSignInFailure.InactiveAdmin);
        }

        // 3. Google account binding: bound on the first login; a different account is refused.
        if (admin.GoogleSub is not null && !admin.IsBoundTo(identity.Subject))
        {
            return GoogleSignInResult.Refused(GoogleSignInFailure.GoogleAccountMismatch);
        }

        if (admin.GoogleSub is null)
        {
            if (await store.IsGoogleAccountBoundToAnotherAdminAsync(identity.Subject, admin.Id, cancellationToken))
            {
                return GoogleSignInResult.Refused(GoogleSignInFailure.GoogleAccountBoundToAnotherAdmin);
            }

            admin.BindGoogleAccount(identity.Subject);
        }

        // 4. Record the login, then the caller issues the session cookie.
        var now = timeProvider.GetUtcNow();
        admin.RecordLogin(now);
        await store.SaveChangesAsync(cancellationToken);

        var name = string.IsNullOrWhiteSpace(identity.Name) ? null : identity.Name.Trim();
        return GoogleSignInResult.Success(new AdminSession(admin.Id, admin.Email, admin.SessionVersion, name, now));
    }

    // Google sends a JSON boolean; anything other than an explicit true (missing, "false", garbage) is refused.
    private static bool IsTrue(string? value) => bool.TryParse(value, out var result) && result;
}
