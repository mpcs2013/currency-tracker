using System.ComponentModel.DataAnnotations;

namespace CurrencyTracker.Infrastructure.Providers;

/// <summary>
/// Strongly-typed configuration for the Frankfurter exchange-rate
/// integration. Bound from the <c>Frankfurter</c> configuration section
/// in <c>AddInfrastructure</c> and validated at host startup via
/// <c>ValidateOnStart()</c>, so a missing or malformed value fails the
/// host at boot rather than on the first request.
/// </summary>
public sealed class FrankfurterOptions
{
    /// <summary>
    /// Configuration section name this options class binds from.
    /// </summary>
    public const string SectionName = "Frankfurter";

    /// <summary>
    /// Absolute base address of the Frankfurter API (for example
    /// <c>https://api.frankfurter.dev</c>). Must be an absolute
    /// <c>https</c> URI — enforced both by <see cref="RequiredAttribute"/>
    /// and by an explicit scheme check in the registration.
    /// </summary>
    [Required]
    public Uri BaseUrl { get; set; } = null!;

    /// <summary>
    /// Per-request timeout applied to the typed client. Defaults to ten
    /// seconds. The resilience pipeline (issue 9.3) applies its own
    /// per-attempt timeout inside this ceiling.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Value sent as the <c>User-Agent</c> request header. A descriptive,
    /// non-empty UA is good external-API citizenship and makes the
    /// project's traffic identifiable in the provider's logs.
    /// </summary>
    [Required]
    public string UserAgent { get; set; } = null!;
}
