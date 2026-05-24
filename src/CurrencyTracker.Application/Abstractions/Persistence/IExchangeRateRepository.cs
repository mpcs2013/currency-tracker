using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.Abstractions.Persistence;

/// <summary>
/// Read-side repository contract for looking up <see cref="ExchangeRate"/>
/// entities by their composite identity.
/// </summary>
public interface IExchangeRateRepository
{
    /// <summary>
    /// Retrieves the snapshot for <paramref name="baseCurrency"/> on
    /// <paramref name="asOf"/>, or <see langword="null"/> if no
    /// snapshot has been persisted for that pair.
    /// </summary>
    /// <param name="baseCurrency">The base currency of the snapshot.</param>
    /// <param name="asOf">The calendar date of the snapshot.</param>
    /// <param name="cancellationToken">Token to cancel the underlying I/O.</param>
    /// <returns>The snapshot with its owned rates loaded, or
    /// <see langword="null"/>.</returns>
    Task<RateSnapshot?> GetSnapshotAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Tracks <paramref name="snapshot"/> for upsert on the next
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> call. If a snapshot
    /// for the same <c>(Base, AsOf)</c> already exists, the Phase 8
    /// adapter replaces it.
    /// </summary>
    /// <param name="snapshot">The snapshot to save.</param>
    /// <param name="cancellationToken">Token to cancel the underlying I/O.</param>
    Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken);
}
