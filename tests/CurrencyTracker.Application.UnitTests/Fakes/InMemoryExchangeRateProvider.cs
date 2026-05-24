using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IExchangeRateProvider"/> fake for unit tests.
/// Snapshots are keyed by base currency and date and can be overridden
/// by <see cref="FailWith"/> to simulate provider failures.
/// </summary>
public sealed class InMemoryExchangeRateProvider : IExchangeRateProvider
{
    private readonly Dictionary<
        (CurrencyCode BaseCurrency, DateOnly AsOf),
        RateSnapshot
    > _snapshots = [];

    /// <summary>
    /// Gets or sets a failure factory that, when configured, overrides
    /// seeded snapshots and is returned from <see cref="FetchAsync"/>.
    /// </summary>
    public Func<Result<RateSnapshot>>? FailWith { get; set; }

    /// <summary>
    /// Seeds a snapshot for the given base currency and date.
    /// </summary>
    /// <param name="baseCurrency">Base currency key.</param>
    /// <param name="asOf">Observation date key.</param>
    /// <param name="snapshot">Snapshot to return for the key.</param>
    public void Seed(CurrencyCode baseCurrency, DateOnly asOf, RateSnapshot snapshot)
    {
        _snapshots[(baseCurrency, asOf)] = snapshot;
    }

    /// <inheritdoc />
    public Task<Result<RateSnapshot>> FetchAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (FailWith is not null)
        {
            return Task.FromResult(FailWith());
        }

        if (_snapshots.TryGetValue((baseCurrency, asOf), out var snapshot))
        {
            return Task.FromResult(Result<RateSnapshot>.Success(snapshot));
        }

        return Task.FromResult(
            Result<RateSnapshot>.Failure(
                new DomainError(
                    "PROVIDER_UNAVAILABLE",
                    "The exchange-rate provider is unavailable for the requested snapshot."
                )
            )
        );
    }
}
