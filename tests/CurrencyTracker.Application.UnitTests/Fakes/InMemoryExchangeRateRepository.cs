using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IExchangeRateRepository"/> fake. Snapshots
/// are stored against <c>(Base, AsOf)</c> tuples; <see cref="SaveSnapshotAsync"/>
/// upserts.
/// </summary>
public sealed class InMemoryExchangeRateRepository : IExchangeRateRepository
{
    private readonly Dictionary<(CurrencyCode, DateOnly), RateSnapshot> _store = new();

    /// <summary>Times GetLatestSnapshotAsync was called — a test hook for the cache-hit assertion.</summary>
    public int LatestReads { get; private set; }

    /// <inheritdoc />
    public Task<RateSnapshot?> GetSnapshotAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _store.TryGetValue((baseCurrency, asOf), out var snap) ? snap : null
        );
    }

    /// <inheritdoc />
    public Task<RateSnapshot?> GetLatestSnapshotAsync(
        CurrencyCode baseCurrency,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        LatestReads++;
        var latest = _store
            .Where(kvp => kvp.Key.Item1.Equals(baseCurrency))
            .OrderByDescending(kvp => kvp.Key.Item2)
            .Select(kvp => kvp.Value)
            .FirstOrDefault();
        return Task.FromResult<RateSnapshot?>(latest);
    }

    /// <inheritdoc />
    public Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[(snapshot.Base, snapshot.AsOf)] = snapshot;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RateSnapshot>> GetSnapshotsInRangeAsync(
        CurrencyCode baseCurrency,
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = _store
            .Where(kvp =>
                kvp.Key.Item1.Equals(baseCurrency)
                && kvp.Key.Item2 >= fromInclusive
                && kvp.Key.Item2 <= toInclusive
            )
            .OrderBy(kvp => kvp.Key.Item2)
            .Select(kvp => kvp.Value)
            .ToList();
        return Task.FromResult<IReadOnlyList<RateSnapshot>>(list);
    }
}
