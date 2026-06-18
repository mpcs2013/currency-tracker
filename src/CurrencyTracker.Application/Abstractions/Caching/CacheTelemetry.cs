using System.Diagnostics.Metrics;

namespace CurrencyTracker.Application.Caching;

/// <summary>
/// Cache telemetry on the shared <c>CurrencyTracker</c> meter scope
/// (registered with OpenTelemetry in Phase 9.10). Defines the
/// <c>cache.hit</c> and <c>cache.miss</c> counters incremented by the cache
/// adapter. Reusing the existing scope means no new OTel registration.
/// </summary>
public static class CacheTelemetry
{
    /// <summary>The instrumentation scope name — the shared app meter (9.10).</summary>
    public const string SourceName = "CurrencyTracker";

    private static readonly Meter Meter = new(SourceName);

    /// <summary>Counter incremented on a cache hit.</summary>
    public static readonly Counter<long> Hits = Meter.CreateCounter<long>(
        "cache.hit",
        unit: "{hit}",
        description: "Number of cache hits served without a source read."
    );

    /// <summary>Counter incremented on a cache miss (the factory ran).</summary>
    public static readonly Counter<long> Misses = Meter.CreateCounter<long>(
        "cache.miss",
        unit: "{miss}",
        description: "Number of cache misses that fell through to the source."
    );
}
