using System.Text.Json;
using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace CurrencyTracker.Infrastructure.Caching;

/// <summary>
/// Redis-backed <see cref="ICacheService"/> adapter over the
/// <see cref="IDistributedCache"/> registered in Phase 10.1. Honours the
/// Phase 4 contract: values are <see cref="string"/>-shaped, JSON
/// serialisation is confined to <see cref="GetOrSetAsync{T}"/>, the TTL is
/// a relative <see cref="TimeSpan"/>, and a ±10% jitter is applied to the
/// stored expiry to avoid synchronised cache stampedes. Behaviour matches
/// the <c>InMemoryCacheService</c> fake the Application unit tests use.
/// </summary>
internal sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken) =>
        cache.GetStringAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task SetAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken
    )
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Jitter(ttl),
        };

        return cache.SetStringAsync(key, value, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        cache.RemoveAsync(key, cancellationToken);

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken
    )
    {
        var cached = await GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<T>(cached)
                ?? throw new InvalidOperationException(
                    $"Cached value for key '{key}' deserialised to null."
                );
        }

        CacheTelemetry.Misses.Add(1);
        var fresh = await factory(cancellationToken).ConfigureAwait(false);
        var serialised = JsonSerializer.Serialize(fresh);
        await SetAsync(key, serialised, ttl, cancellationToken).ConfigureAwait(false);
        return fresh;
    }

    // ±10% jitter so simultaneously-set keys don't all expire at the same
    // instant and stampede the source on the next read. Adapter concern,
    // invisible to callers (Phase 4 ICacheService design).
    private static TimeSpan Jitter(TimeSpan ttl)
    {
        var factor = 1.0 + ((Random.Shared.NextDouble() * 0.2) - 0.1); // [0.9, 1.1)
        return ttl * factor;
    }
}
