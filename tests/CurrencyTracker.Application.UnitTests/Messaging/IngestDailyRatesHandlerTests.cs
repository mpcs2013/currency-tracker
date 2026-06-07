using CurrencyTracker.Application.Caching;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Exceptions;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Messaging;

public sealed class IngestDailyRatesHandlerTests
{
    private static readonly CancellationToken NoCt = CancellationToken.None;
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly DateOnly AsOf = new DateOnly(2026, 5, 28);
    private static RateSnapshot SampleSnapshot() =>
        RateSnapshot.Create(Usd, AsOf, [ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value]).Value;

    [Fact]
    public async Task Handle_evicts_latest_rates_key_after_successful_ingest()
    {
        // Arrange
        var provider = new InMemoryExchangeRateProvider();
        provider.Seed(Usd, AsOf, SampleSnapshot());
        var repo = new InMemoryExchangeRateRepository();
        var uow = new InMemoryUnitOfWork();
        var cache = new InMemoryCacheService();
        // pre-seed the key so we can prove it was removed
        await cache.SetAsync(
            CacheKeys.LatestRates("USD"),
            "stale",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken
        );

        // Act
        await IngestDailyRatesHandler.Handle(
            new IngestDailyRatesCommand("USD", AsOf),
            provider,
            repo,
            uow,
            cache,
            TestContext.Current.CancellationToken
        );

        // Assert
        var afterIngest = await cache.GetAsync(
            CacheKeys.LatestRates("USD"),
            TestContext.Current.CancellationToken
        );
        afterIngest.Should().BeNull(); // the key was evicted
    }

    [Fact]
    public async Task Handle_does_not_evict_when_provider_fails()
    {
        // Arrange
        var provider = new InMemoryExchangeRateProvider
        {
            FailWith = new DomainError("PROVIDER_UNAVAILABLE", "down"),
        };
        var repo = new InMemoryExchangeRateRepository();
        var uow = new InMemoryUnitOfWork();
        var cache = new InMemoryCacheService();
        await cache.SetAsync(
            CacheKeys.LatestRates("USD"),
            "stale",
            TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken
        );

        // Act
        var act = () =>
            IngestDailyRatesHandler.Handle(
                new IngestDailyRatesCommand("USD", AsOf),
                provider,
                repo,
                uow,
                cache,
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        var stillCached = await cache.GetAsync(
            CacheKeys.LatestRates("USD"),
            TestContext.Current.CancellationToken
        );
        stillCached.Should().Be("stale"); // failure before commit → no eviction
    }
}
