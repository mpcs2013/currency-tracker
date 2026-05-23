using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.Rates;

/// <summary>
/// Read-side repository contract for looking up <see cref="ExchangeRate"/>
/// entities by their composite identity.
/// </summary>
public interface IExchangeRateRepository
{
    /// <summary>
    /// Gets an exchange rate by base/quote pair and observation date.
    /// </summary>
    /// <param name="base">Base currency code.</param>
    /// <param name="quote">Quote currency code.</param>
    /// <param name="asOf">Observation date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching exchange rate or <see langword="null"/> when absent.</returns>
#pragma warning disable CA1716 // Required domain ubiquitous language: base currency.
    Task<ExchangeRate?> GetAsync(
        CurrencyCode @base,
        CurrencyCode quote,
        DateOnly asOf,
        CancellationToken cancellationToken
    );
#pragma warning restore CA1716

    // Phase 3.5: uncomment snapshot-oriented methods when RateSnapshot lands.
    // Task<IReadOnlyList<ExchangeRate>> ListForSnapshotAsync(
    //     CurrencyCode @base,
    //     DateOnly asOf,
    //     CancellationToken cancellationToken
    // );
    // Task UpsertSnapshotAsync(
    //     IReadOnlyCollection<ExchangeRate> rates,
    //     CancellationToken cancellationToken
    // );
}
