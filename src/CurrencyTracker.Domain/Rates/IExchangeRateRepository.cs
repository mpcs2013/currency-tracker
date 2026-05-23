using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.Rates;

/// <summary>
/// Read-side repository contract for looking up <see cref="ExchangeRate"/>
/// entities by their composite identity.
/// </summary>
public interface IExchangeRateRepository
{
    /// <summary>
    /// Gets the exchange rate between two currencies for a specific date.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="quote">The quote currency code.</param>
    /// <param name="asOf">The date for which to retrieve the exchange rate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The exchange rate for the specified currencies and date, or <see langword="null"/> if not found.</returns>
    Task<ExchangeRate?> GetAsync(
        CurrencyCode baseCurrency,
        CurrencyCode quote,
        DateOnly asOf,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the exchange rate snapshot for a specific base currency and date.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="asOf">The date for which to retrieve the snapshot.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The exchange rate snapshot for the specified base currency and date, or <see langword="null"/> if not found.</returns>
    Task<RateSnapshot?> GetSnapshotAsync(CurrencyCode baseCurrency, DateOnly asOf, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the provided exchange rate snapshot.
    /// </summary>
    /// <param name="snapshot">The exchange rate snapshot to save.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task SaveSnapshotAsync(RateSnapshot snapshot, CancellationToken cancellationToken);
}
