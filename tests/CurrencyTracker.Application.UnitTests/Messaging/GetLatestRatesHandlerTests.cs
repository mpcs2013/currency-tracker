using CurrencyTracker.Application.Caching;
using CurrencyTracker.Application.Exceptions;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using FluentAssertions;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="GetLatestRatesHandler"/> against the Phase 4
/// fakes (in-memory cache + repository) — no Redis, no Postgres.
/// </summary>
public sealed class GetLatestRatesHandlerTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly DateOnly AsOf = new(2026, 5, 28);

    private static RateSnapshot SampleSnapshot() =>
        RateSnapshot.Create(Usd, AsOf, [ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value]).Value;

    [Fact]
    public async Task Handle_reads_repo_and_caches_on_miss()
    {
        // Arrange
        var repo = new InMemoryExchangeRateRepository();
        await repo.SaveSnapshotAsync(SampleSnapshot(), TestContext.Current.CancellationToken);
        var cache = new InMemoryCacheService();
        var sut = new GetLatestRatesHandler(repo, cache, new GetLatestRatesQueryValidator());

        // Act
        var result = await sut.Handle(
            new GetLatestRatesQuery("USD"),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Should().ContainSingle();
        result[0].Quote.Should().Be("EUR");
        var cached = await cache.GetAsync(
            CacheKeys.LatestRates("USD"),
            TestContext.Current.CancellationToken
        );
        cached.Should().NotBeNull(); // the miss populated the cache
    }

    [Fact]
    public async Task Handle_returns_cached_value_without_touching_repo_on_hit()
    {
        // Arrange
        var repo = new InMemoryExchangeRateRepository();
        await repo.SaveSnapshotAsync(SampleSnapshot(), TestContext.Current.CancellationToken);
        var cache = new InMemoryCacheService();
        var sut = new GetLatestRatesHandler(repo, cache, new GetLatestRatesQueryValidator());
        await sut.Handle(new GetLatestRatesQuery("USD"), TestContext.Current.CancellationToken);
        repo.LatestReads.Should().Be(1);

        // Act
        await sut.Handle(new GetLatestRatesQuery("USD"), TestContext.Current.CancellationToken);

        // Assert
        repo.LatestReads.Should().Be(1); // second call served from cache, not the repo
    }

    [Fact]
    public async Task Handle_throws_NotFound_and_caches_nothing_when_no_snapshot()
    {
        // Arrange
        var repo = new InMemoryExchangeRateRepository(); // empty
        var cache = new InMemoryCacheService();
        var sut = new GetLatestRatesHandler(repo, cache, new GetLatestRatesQueryValidator());

        // Act
        var act = () =>
            sut.Handle(new GetLatestRatesQuery("USD"), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        var cached = await cache.GetAsync(
            CacheKeys.LatestRates("USD"),
            TestContext.Current.CancellationToken
        );
        cached.Should().BeNull(); // a 404 caches nothing
    }
}
