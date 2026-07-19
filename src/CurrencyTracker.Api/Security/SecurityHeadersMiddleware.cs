using Microsoft.Extensions.Options;

namespace CurrencyTracker.Api.Security;

/// <summary>
/// Writes the static security headers on every response. Runs first in the
/// pipeline and sets the headers before calling <c>next</c>, so they are
/// present even on responses produced by short-circuiting middleware (a 429
/// from the rate limiter, a 401 from status-code pages, a 500 from the
/// exception handler). Uses the indexer (assignment, not <c>Add</c>) so a
/// value is set-or-overwritten rather than throwing on a duplicate key.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="options">The configured header values.</param>
internal sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IOptions<SecurityHeaderOptions> options
)
{
    private readonly SecurityHeaderOptions _options = options.Value;

    /// <summary>Sets the headers and invokes the rest of the pipeline.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that completes when the pipeline completes.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = _options.ReferrerPolicy;
        headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

        return next(context);
    }
}
