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
    public Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[(snapshot.Base, snapshot.AsOf)] = snapshot;
        return Task.CompletedTask;
    }
}
