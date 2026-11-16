using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SilvaStock.Infrastructure.Authentication;

/// <summary>
/// Session cookie events (ADR-0004, Session). API calls never redirect: a challenge is a <c>401</c> and a forbid a
/// <c>403</c>, both as ProblemDetails without a <c>Location</c> header.
/// </summary>
internal sealed class SessionCookieEvents(IProblemDetailsService problemDetails) : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden);
    }

    private async Task WriteProblemAsync(HttpContext httpContext, int statusCode)
    {
        httpContext.Response.StatusCode = statusCode;
        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = statusCode },
        });
    }
}
