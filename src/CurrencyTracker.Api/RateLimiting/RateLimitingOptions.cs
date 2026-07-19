namespace CurrencyTracker.Api.RateLimiting;

/// <summary>
/// Strongly-typed rate-limit configuration, bound from the
/// <c>RateLimiting</c> section. Every value is a tuning knob; none is
/// hardcoded in the limiter wiring.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>The per-IP fixed-window limits.</summary>
    public PerIpOptions PerIp { get; init; } = new();

    /// <summary>The per-user token-bucket limits.</summary>
    public PerUserOptions PerUser { get; init; } = new();

    /// <summary>Per-IP fixed-window settings (the coarse DoS backstop).</summary>
    public sealed class PerIpOptions
    {
        /// <summary>Maximum requests permitted per IP per window.</summary>
        public int PermitLimit { get; init; } = 100;

        /// <summary>Window length in seconds.</summary>
        public int WindowSeconds { get; init; } = 60;
    }

    /// <summary>Per-user token-bucket settings (the fair-use ceiling).</summary>
    public sealed class PerUserOptions
    {
        /// <summary>Maximum tokens (burst ceiling) per user.</summary>
        public int TokenLimit { get; init; } = 120;

        /// <summary>Tokens replenished each replenishment period.</summary>
        public int TokensPerPeriod { get; init; } = 20;

        /// <summary>Replenishment cadence in seconds.</summary>
        public int ReplenishmentPeriodSeconds { get; init; } = 10;
    }
}
