namespace CurrencyTracker.Api.Security;

/// <summary>
/// Configurable security-header values, bound from the
/// <c>SecurityHeaders</c> section. <c>X-Content-Type-Options</c> is not here
/// — it has one correct value (<c>nosniff</c>) and is a constant in the
/// middleware.
/// </summary>
public sealed class SecurityHeaderOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "SecurityHeaders";

    /// <summary>The Content-Security-Policy value.</summary>
    public string ContentSecurityPolicy { get; init; } =
        "default-src 'self'; frame-ancestors 'none'";

    /// <summary>The Referrer-Policy value.</summary>
    public string ReferrerPolicy { get; init; } = "no-referrer";

    /// <summary>The HSTS max-age, in days (consumed by UseHsts configuration).</summary>
    public int HstsMaxAgeDays { get; init; } = 365;
}
