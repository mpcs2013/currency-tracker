using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.Abstractions.Providers;

/// <summary>
/// Retrieves exchange-rate snapshots from an external provider without
/// exposing transport-specific wire shapes to Application.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Fetches the provider snapshot for the supplied base currency and date.
    /// </summary>
    /// <param name="baseCurrency">Base currency of the requested snapshot.</param>
    /// <param name="asOf">Observation date for the requested snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel underlying I/O.</param>
    /// <returns>
    /// Success carrying a <see cref="RateSnapshot"/> when available, or
    /// failure carrying a <see cref="DomainError"/> such as
    /// <c>PROVIDER_UNAVAILABLE</c> or <c>PROVIDER_UNSUPPORTED_CURRENCY</c>.
    /// </returns>
    Task<Result<RateSnapshot>> FetchAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    );
}
