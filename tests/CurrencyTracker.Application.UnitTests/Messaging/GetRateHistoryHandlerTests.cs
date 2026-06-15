using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>Unit tests for <see cref="GetRateHistoryHandler"/> against the in-memory fake.</summary>
public sealed class GetRateHistoryHandlerTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    private static RateSnapshot SnapshotOn(DateOnly asOf, decimal rate) =>
        RateSnapshot.Create(Usd, asOf, [ExchangeRate.Create(Usd, Eur, rate, asOf).Value]).Value;

    [Fact]
    public async Task Handle_returns_points_ascending_by_date()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var repo = new InMemoryExchangeRateRepository();
        await repo.SaveSnapshotAsync(SnapshotOn(new(2026, 5, 3), 0.92m), ct); // out of order on purpose
        await repo.SaveSnapshotAsync(SnapshotOn(new(2026, 5, 1), 0.90m), ct);
        await repo.SaveSnapshotAsync(SnapshotOn(new(2026, 5, 2), 0.91m), ct);
        var sut = new GetRateHistoryHandler(repo);

        // Act
        var result = await sut.Handle(
            new GetRateHistoryQuery("USD", "EUR", new(2026, 5, 1), new(2026, 5, 31)),
            ct
        );

        // Assert
        result.Should().HaveCount(3);
        result.Select(p => p.AsOf).Should().BeInAscendingOrder();
        result[0].Rate.Should().Be(0.90m);
        result[2].Rate.Should().Be(0.92m);
    }

    [Fact]
    public async Task Handle_returns_empty_list_when_no_snapshots_in_range()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var repo = new InMemoryExchangeRateRepository(); // empty
        var sut = new GetRateHistoryHandler(repo);

        // Act
        var result = await sut.Handle(
            new GetRateHistoryQuery("USD", "EUR", new(2026, 5, 1), new(2026, 5, 31)),
            ct
        );

        // Assert
        result.Should().BeEmpty(); // empty range → 200 [] at the endpoint, never 404
    }
}
