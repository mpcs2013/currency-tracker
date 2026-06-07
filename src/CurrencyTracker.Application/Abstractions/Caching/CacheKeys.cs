namespace CurrencyTracker.Application.Caching;

/// <summary>
/// Cache-key conventions shared by readers and invalidators so the two
/// can't drift. The format is documented in <c>docs/caching.md</c>. Keys
/// carry only non-sensitive identifiers (currency codes) — never PII or
/// tokens (see the Phase 10.11 security review).
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Key for the latest-rates read model of a base currency, e.g.
    /// <c>"rates:latest:USD"</c>.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <returns>The cache key.</returns>
    public static string LatestRates(string baseCurrency) => $"rates:latest:{baseCurrency}";
}
