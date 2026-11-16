using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SilvaStock.Infrastructure.Authentication;

/// <summary>
/// Starts the Google login (<c>GET /api/auth/login/google</c>). The login is a full-page navigation, so it always
/// redirects: to Google, or to <c>{site}/admin/login?error=login_failed</c> when the challenge cannot be issued
/// (e.g. Google's discovery document cannot be loaded). Only the exception type is logged.
/// </summary>
public sealed partial class GoogleLoginStarter(LoginRedirects redirects, ILogger<GoogleLoginStarter> logger)
{
    /// <summary>
    /// Challenges the Google scheme explicitly. The sanitised <c>{site}{returnUrl}</c> travels only inside the
    /// protected OIDC state and is used only after a successful login.
    /// </summary>
    public async Task StartAsync(HttpContext httpContext, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var properties = new AuthenticationProperties { RedirectUri = redirects.ReturnTarget(returnUrl) };
        try
        {
            await httpContext.ChallengeAsync(AdminAuthenticationDefaults.GoogleScheme, properties);
        }
        catch (Exception exception) when (!httpContext.RequestAborted.IsCancellationRequested && !httpContext.Response.HasStarted)
        {
            LogStartFailed(logger, LoginErrorCodes.LoginFailed, exception.GetType().Name);
            httpContext.Response.Clear();
            httpContext.Response.Redirect(redirects.LoginError(LoginErrorCodes.LoginFailed));
        }
    }

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Google login could not start: {ErrorCode} ({Reason})")]
    private static partial void LogStartFailed(ILogger logger, string errorCode, string reason);
}
