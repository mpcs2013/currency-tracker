namespace CurrencyTracker.Application.Abstractions.Caching;

/// <summary>
/// Abstraction over a key-value cache. The Phase 10 adapter implements
/// this against Redis via StackExchange.Redis; unit tests use an
/// in-memory fake. Cache values are <see cref="string"/>-shaped at the
/// interface level — typed serialisation is handled inside
/// <see cref="GetOrSetAsync{T}"/>.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets the raw cache value for <paramref name="key"/>, or
    /// <see langword="null"/> if the key is absent or expired.
    /// </summary>
    /// <param name="key">Cache key. Format is the caller's concern; see
    /// the cache-key convention in <c>docs/caching.md</c> when Phase 10
    /// adds it.</param>
    /// <param name="cancellationToken">Token to cancel the underlying
    /// I/O. Honoured in the Phase 10 Redis adapter; harmless in the
    /// in-memory fake.</param>
    /// <returns>The cached value, or <see langword="null"/> if not
    /// present.</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores <paramref name="value"/> under <paramref name="key"/>
    /// with a relative time-to-live.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to store. <see cref="string"/>-shaped;
    /// callers serialise typed payloads themselves or use
    /// <see cref="GetOrSetAsync{T}"/>.</param>
    /// <param name="ttl">Relative TTL. The adapter is free to add
    /// jitter (±10% in Phase 10's Redis adapter) so simultaneous
    /// expiries don't stampede the underlying source.</param>
    /// <param name="cancellationToken">Token to cancel the underlying I/O.</param>
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Removes <paramref name="key"/> from the cache if present;
    /// otherwise no-op.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Token to cancel the underlying I/O.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Cache-aside helper: returns the cached value for
    /// <paramref name="key"/> if present; otherwise invokes
    /// <paramref name="factory"/>, caches its result with
    /// <paramref name="ttl"/>, and returns the freshly-produced value.
    /// </summary>
    /// <typeparam name="T">The typed shape of the cached value. JSON
    /// serialisation between <typeparamref name="T"/> and the
    /// underlying <see cref="string"/> store is the adapter's
    /// responsibility.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Async producer of the typed value when
    /// the cache misses. Must honour the supplied
    /// <see cref="CancellationToken"/>.</param>
    /// <param name="ttl">Relative TTL for the cached value.</param>
    /// <param name="cancellationToken">Token to cancel both the cache
    /// I/O and the factory.</param>
    /// <returns>The cached or freshly-produced typed value.</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken
    );
}
