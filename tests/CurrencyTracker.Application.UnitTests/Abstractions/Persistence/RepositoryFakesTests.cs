using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Abstractions;

/// <summary>
/// Tests exercising the persistence-port fakes (currency repository,
/// exchange-rate repository, unit of work).
/// </summary>
public sealed class RepositoryFakesTests
{
    private static readonly CancellationToken NoCt = CancellationToken.None;
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    [Fact]
    public async Task InMemoryCurrencyRepository_AddAsync_then_GetByCodeAsync_round_trips()
    {
        var sut = new InMemoryCurrencyRepository();
        var currency = Currency.Create(Usd, "United States Dollar", 840).Value;

        await sut.AddAsync(currency, NoCt);
        var actual = await sut.GetByCodeAsync(Usd, NoCt);

        actual.Should().NotBeNull().And.BeSameAs(currency);
    }

    [Fact]
    public async Task InMemoryCurrencyRepository_GetByCodeAsync_returns_null_for_missing()
    {
        var sut = new InMemoryCurrencyRepository();

        var actual = await sut.GetByCodeAsync(Eur, NoCt);

        actual.Should().BeNull();
    }

    [Fact]
    public async Task InMemoryExchangeRateRepository_SaveSnapshotAsync_then_GetSnapshotAsync_round_trips()
    {
        var sut = new InMemoryExchangeRateRepository();
        var asOf = new DateOnly(2026, 5, 21);
        var snapshot = RateSnapshot
            .Create(Usd, asOf, [ExchangeRate.Create(Usd, Eur, 0.92m, asOf).Value])
            .Value;

        await sut.SaveSnapshotAsync(snapshot, NoCt);
        var actual = await sut.GetSnapshotAsync(Usd, asOf, NoCt);

        actual.Should().NotBeNull().And.BeSameAs(snapshot);
    }

    [Fact]
    public async Task InMemoryExchangeRateRepository_GetSnapshotAsync_returns_null_for_missing()
    {
        var sut = new InMemoryExchangeRateRepository();

        var actual = await sut.GetSnapshotAsync(Usd, new DateOnly(2026, 5, 21), NoCt);

        actual.Should().BeNull();
    }

    [Fact]
    public async Task RecordingUnitOfWork_SaveChangesAsync_increments_SaveCount()
    {
        var sut = new RecordingUnitOfWork();

        await sut.SaveChangesAsync(NoCt);
        await sut.SaveChangesAsync(NoCt);

        sut.SaveCount.Should().Be(2);
    }

    [Fact]
    public async Task RecordingUnitOfWork_SaveChangesAsync_honours_cancellation_token()
    {
        var sut = new RecordingUnitOfWork();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.SaveChangesAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
