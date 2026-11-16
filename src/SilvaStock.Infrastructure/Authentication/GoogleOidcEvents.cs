using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using SilvaStock.Application.Auth;

namespace SilvaStock.Infrastructure.Authentication;

/// <summary>
/// Google OIDC events (ADR-0004, Login). The allowlist rules run in <see cref="TicketReceived"/>, i.e. only after the
/// handler has completed every protocol check (state, correlation, PKCE code redemption, <c>id_token</c> signature,
/// issuer, audience, lifetime and nonce). <c>OnTokenValidated</c> is deliberately not used: the handler runs it before
/// the nonce is validated. They decide between a session and <c>not_allowed</c>; every remote failure becomes
/// <c>login_failed</c>. Google's error text and exception messages are never reflected or logged.
/// </summary>
internal sealed partial class GoogleOidcEvents(
    GoogleSignInService signInService,
    LoginRedirects redirects,
    ILogger<GoogleOidcEvents> logger) : OpenIdConnectEvents
{
    public override async Task TicketReceived(TicketReceivedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var token = context.Principal ?? new ClaimsPrincipal();
        var identity = new GoogleIdentity(
            Subject: token.FindFirst("sub")?.Value,
            Email: token.FindFirst("email")?.Value,
            EmailVerified: token.FindFirst("email_verified")?.Value,
            Name: token.FindFirst("name")?.Value);

        GoogleSignInResult result;
        try
        {
            result = await signInService.SignInAsync(identity, context.HttpContext.RequestAborted);
        }
        catch (Exception exception) when (!context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // E.g. database unavailable or timing out, or a concurrent google_sub binding. Exceptions here are not
            // routed to OnRemoteFailure by the handler, so they are mapped to login_failed explicitly (type name
            // only). Only a cancellation caused by the client aborting the request is allowed to propagate.
            LogLoginFailed(logger, LoginErrorCodes.LoginFailed, exception.GetType().Name);
            Redirect(context, LoginErrorCodes.LoginFailed);
            return;
        }

        if (result.Session is not { } session)
        {
            LogLoginRefused(logger, LoginErrorCodes.NotAllowed, result.Failure?.ToString() ?? "Unknown", EmailDomain(identity.Email));
            Redirect(context, LoginErrorCodes.NotAllowed);
            return;
        }

        // Only the session claims and a persistent flag: nothing else from the id_token or the OIDC state is kept.
        context.Principal = CreateSessionPrincipal(session);
        context.Properties = new AuthenticationProperties { IsPersistent = true };
        context.ReturnUri = redirects.RevalidateReturnTarget(context.ReturnUri);

        LogLoginSucceeded(logger, session.AdminId);
    }

    public override Task AccessDenied(AccessDeniedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LogLoginFailed(logger, LoginErrorCodes.LoginFailed, "AccessDenied");
        context.HandleResponse();
        context.Response.Redirect(redirects.LoginError(LoginErrorCodes.LoginFailed));
        return Task.CompletedTask;
    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Only the exception type: messages may echo Google's error_description or protocol values.
        LogLoginFailed(logger, LoginErrorCodes.LoginFailed, context.Failure?.GetType().Name ?? "Unknown");
        context.HandleResponse();
        context.Response.Redirect(redirects.LoginError(LoginErrorCodes.LoginFailed));
        return Task.CompletedTask;
    }

    private void Redirect(TicketReceivedContext context, string errorCode)
    {
        context.HandleResponse();
        context.Response.Redirect(redirects.LoginError(errorCode));
    }

    internal static ClaimsPrincipal CreateSessionPrincipal(AdminSession session)
    {
        var claims = new List<Claim>
        {
            new(SessionClaimTypes.AdminId, session.AdminId.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(SessionClaimTypes.Email, session.Email, ClaimValueTypes.String),
            new(SessionClaimTypes.SessionVersion, session.SessionVersion.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer32),
            new(SessionClaimTypes.IssuedAt, session.IssuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
        };

        if (session.GoogleName is not null)
        {
            claims.Add(new Claim(SessionClaimTypes.Name, session.GoogleName, ClaimValueTypes.String));
        }

        var identity = new ClaimsIdentity(
            claims,
            AdminAuthenticationDefaults.SessionScheme,
            nameType: SessionClaimTypes.Email,
            roleType: null);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>At most the email domain is logged (ADR-0004, Management and logging).</summary>
    internal static string EmailDomain(string? email)
    {
        var at = email?.LastIndexOf('@') ?? -1;
        return email is null || at < 0 || at == email.Length - 1
            ? "(none)"
            : email[(at + 1)..].Trim().ToLowerInvariant();
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Google login succeeded for admin {AdminId}")]
    private static partial void LogLoginSucceeded(ILogger logger, long adminId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Google login refused: {ErrorCode} ({Reason}), email domain {EmailDomain}")]
    private static partial void LogLoginRefused(ILogger logger, string errorCode, string reason, string emailDomain);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Google login failed: {ErrorCode} ({Reason})")]
    private static partial void LogLoginFailed(ILogger logger, string errorCode, string reason);
}
