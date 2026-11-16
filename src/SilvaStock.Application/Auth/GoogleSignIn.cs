namespace SilvaStock.Application.Auth;

/// <summary>Claims of a Google <c>id_token</c> that has already been validated (signature, issuer, audience, nonce).</summary>
/// <param name="Subject">The <c>sub</c> claim (stable Google account id).</param>
/// <param name="Email">The <c>email</c> claim, as sent by Google.</param>
/// <param name="EmailVerified">The raw <c>email_verified</c> claim value; null when the claim is missing.</param>
/// <param name="Name">The <c>name</c> claim (display-name fallback; never persisted).</param>
public sealed record GoogleIdentity(string? Subject, string? Email, string? EmailVerified, string? Name);

/// <summary>Why a Google sign-in was refused. Every reason maps to the same public error code (<c>not_allowed</c>).</summary>
public enum GoogleSignInFailure
{
    /// <summary>The token has no <c>sub</c> or no <c>email</c>.</summary>
    MissingClaims,

    /// <summary><c>email_verified</c> is missing or not <c>true</c>.</summary>
    EmailNotVerified,

    /// <summary>The email is not on the allowlist.</summary>
    UnknownEmail,

    /// <summary>The administrator exists but is inactive.</summary>
    InactiveAdmin,

    /// <summary>The administrator is bound to a different Google account.</summary>
    GoogleAccountMismatch,

    /// <summary>The Google account is already bound to another administrator.</summary>
    GoogleAccountBoundToAnotherAdmin,
}

/// <summary>Data placed in the session cookie after a successful sign-in (ADR-0004, Session).</summary>
public sealed record AdminSession(long AdminId, string Email, int SessionVersion, string? GoogleName, DateTimeOffset IssuedAt);

/// <summary>Outcome of <see cref="GoogleSignInService.SignInAsync"/>: either a session or a failure reason.</summary>
public sealed class GoogleSignInResult
{
    private GoogleSignInResult(AdminSession? session, GoogleSignInFailure? failure)
    {
        Session = session;
        Failure = failure;
    }

    /// <summary>The session to issue; null when the sign-in was refused.</summary>
    public AdminSession? Session { get; }

    /// <summary>The refusal reason (for logs only); null on success.</summary>
    public GoogleSignInFailure? Failure { get; }

    public bool Succeeded => Session is not null;

    public static GoogleSignInResult Success(AdminSession session) => new(session, null);

    public static GoogleSignInResult Refused(GoogleSignInFailure failure) => new(null, failure);
}
