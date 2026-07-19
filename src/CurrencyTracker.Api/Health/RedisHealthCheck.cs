using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CurrencyTracker.Api.Health;

/// <summary>
/// Readiness check that reports whether the Api can reach Redis. Probes the
/// same <see cref="IDistributedCache"/> the Phase 10 cache path uses, so a
/// healthy answer means the cache path itself is reachable. A read for a
/// missing key is a full round-trip; if Redis is down the read throws, and
/// the health-check framework reports the check <c>Unhealthy</c> — so there
/// is deliberately no broad <c>catch</c> here.
/// </summary>
/// <param name="cache">The distributed cache registered in Phase 10.</param>
internal sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    private const string ProbeKey = "health:ready:redis-probe";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken
    )
    {
        _ = await cache.GetAsync(ProbeKey, cancellationToken);

        return HealthCheckResult.Healthy("Redis is reachable.");
    }
}
