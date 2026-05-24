using System.Collections.Concurrent;
using System.Text.Json;
using CurrencyTracker.Application.Abstractions.Caching;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="ICacheService"/> fake backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Expiry is checked
/// lazily on read; expired entries are removed when observed.
/// JSON serialisation in <see cref="GetOrSetAsync{T}"/> uses the BCL
/// <see cref="JsonSerializer"/> with default options.
/// </summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (string Value, DateTimeOffset Expires)> _store =
        new();

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.Expires > DateTimeOffset.UtcNow)
            {
                return Task.FromResult<string?>(entry.Value);
            }

            _store.TryRemove(key, out _);
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken
    )
    {
        _store[key] = (value, DateTimeOffset.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

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

        var fresh = await factory(cancellationToken).ConfigureAwait(false);
        var serialised = JsonSerializer.Serialize(fresh);
        await SetAsync(key, serialised, ttl, cancellationToken).ConfigureAwait(false);
        return fresh;
    }
}
