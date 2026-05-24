using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IExchangeRateProvider"/> fake. Tests seed
/// expected (base, date) → snapshot mappings via <see cref="Seed"/>;
/// unseeded lookups return a <c>PROVIDER_UNAVAILABLE</c> failure.
/// Setting <see cref="FailWith"/> forces every call to return that
/// failure regardless of seed.
/// </summary>
public sealed class InMemoryExchangeRateProvider : IExchangeRateProvider
{
    private readonly Dictionary<(CurrencyCode, DateOnly), RateSnapshot> _store = new();

    /// <summary>
    /// When non-<see langword="null"/>, every <see cref="FetchAsync"/>
    /// call returns a <see cref="Result{T}.Failure"/> carrying this
    /// error, ignoring any seeded snapshot. Use to exercise failure
    /// paths in handler tests.
    /// </summary>
    public DomainError? FailWith { get; set; }

    /// <summary>
    /// Seeds the fake so that <see cref="FetchAsync"/> with the same
    /// <paramref name="baseCurrency"/> and <paramref name="asOf"/>
    /// returns <paramref name="snapshot"/>.
    /// </summary>
    /// <param name="baseCurrency">Base currency key.</param>
    /// <param name="asOf">Calendar date key.</param>
    /// <param name="snapshot">Snapshot to return.</param>
    public void Seed(CurrencyCode baseCurrency, DateOnly asOf, RateSnapshot snapshot) =>
        _store[(baseCurrency, asOf)] = snapshot;

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
            return Task.FromResult(Result<RateSnapshot>.Failure(FailWith));
        }

        if (_store.TryGetValue((baseCurrency, asOf), out var snapshot))
        {
            return Task.FromResult(Result<RateSnapshot>.Success(snapshot));
        }

        return Task.FromResult(
            Result<RateSnapshot>.Failure(
                new DomainError(
                    "PROVIDER_UNAVAILABLE",
                    $"No seeded snapshot for {baseCurrency.Value} on {asOf:yyyy-MM-dd}."
                )
            )
        );
    }
}
