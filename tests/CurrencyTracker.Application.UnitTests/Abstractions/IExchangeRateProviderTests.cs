using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Application.UnitTests.Abstractions;

/// <summary>
/// Tests exercising the IExchangeRateProvider contract via the
/// <see cref="InMemoryExchangeRateProvider"/> fake.
/// </summary>
public sealed class IExchangeRateProviderTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    [Fact]
    public async Task FetchAsync_returns_seeded_snapshot_for_matching_key()
    {
        var sut = new InMemoryExchangeRateProvider();
        var asOf = new DateOnly(2026, 05, 24);
        var snapshot = CreateSnapshot(Usd, Eur, asOf, 0.92m);
        sut.Seed(Usd, asOf, snapshot);

        var result = await sut.FetchAsync(Usd, asOf, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(snapshot);
    }

    [Fact]
    public async Task FetchAsync_returns_failure_when_snapshot_is_not_seeded()
    {
        var sut = new InMemoryExchangeRateProvider();
        var asOf = new DateOnly(2026, 05, 24);

        var result = await sut.FetchAsync(Usd, asOf, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task FetchAsync_returns_PROVIDER_UNAVAILABLE_for_unseeded_key()
    {
        var sut = new InMemoryExchangeRateProvider();
        var asOf = new DateOnly(2026, 05, 24);

        var result = await sut.FetchAsync(Usd, asOf, CancellationToken.None);

        result.Error.Code.Should().Be("PROVIDER_UNAVAILABLE");
    }

    [Fact]
    public async Task FailWith_overrides_seeded_snapshot()
    {
        var sut = new InMemoryExchangeRateProvider();
        var asOf = new DateOnly(2026, 05, 24);
        var snapshot = CreateSnapshot(Usd, Eur, asOf, 0.92m);
        sut.Seed(Usd, asOf, snapshot);
        sut.FailWith = () =>
            Result<RateSnapshot>.Failure(
                new DomainError(
                    "PROVIDER_UNSUPPORTED_CURRENCY",
                    "The provider does not support the requested base currency."
                )
            );

        var result = await sut.FetchAsync(Usd, asOf, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PROVIDER_UNSUPPORTED_CURRENCY");
    }

    [Fact]
    public async Task FetchAsync_honours_cancellation_token()
    {
        var sut = new InMemoryExchangeRateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.FetchAsync(Usd, new DateOnly(2026, 05, 24), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static RateSnapshot CreateSnapshot(
        CurrencyCode baseCurrency,
        CurrencyCode quoteCurrency,
        DateOnly asOf,
        decimal rate
    ) =>
        RateSnapshot
            .Create(
                baseCurrency,
                asOf,
                [ExchangeRate.Create(baseCurrency, quoteCurrency, rate, asOf).Value]
            )
            .Value;
}
