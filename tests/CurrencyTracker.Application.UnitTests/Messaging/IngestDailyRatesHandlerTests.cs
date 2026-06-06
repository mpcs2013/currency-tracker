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

    [Fact]
    public async Task Handle_persists_snapshot_and_returns_event_on_success()
    {
        var provider = new InMemoryExchangeRateProvider();
        var repository = new InMemoryExchangeRateRepository();
        var uow = new InMemoryUnitOfWork();
        var snapshot = RateSnapshot
            .Create(Usd, AsOf, [ExchangeRate.Create(Usd, Eur, 0.92m, AsOf).Value])
            .Value;
        provider.Seed(Usd, AsOf, snapshot);
        var command = new IngestDailyRatesCommand("USD", AsOf);

        var @event = await IngestDailyRatesHandler.Handle(command, provider, repository, uow, NoCt);
        var persisted = await repository.GetSnapshotAsync(Usd, AsOf, NoCt);

        @event.Base.Should().Be(Usd);
        @event.AsOf.Should().Be(AsOf);
        @event.RateCount.Should().Be(1);
        persisted.Should().NotBeNull().And.BeSameAs(snapshot);
        uow.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_throws_when_provider_fails()
    {
        var provider = new InMemoryExchangeRateProvider
        {
            FailWith = new DomainError("PROVIDER_UNAVAILABLE", "down"),
        };
        var repository = new InMemoryExchangeRateRepository();
        var uow = new InMemoryUnitOfWork();
        var command = new IngestDailyRatesCommand("USD", AsOf);

        var act = () => IngestDailyRatesHandler.Handle(command, provider, repository, uow, NoCt);
        var persisted = await repository.GetSnapshotAsync(Usd, AsOf, NoCt);

        await act.Should().ThrowAsync<DomainException>();
        persisted.Should().BeNull();
        uow.SaveCount.Should().Be(0);
    }
}
