using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.UnitTests.Fakes;
using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CurrencyTracker.Application.UnitTests.Abstractions;

/// <summary>
/// Tests exercising the <see cref="IExchangeRateProvider"/> contract
/// via the <see cref="InMemoryExchangeRateProvider"/> fake: seeded
/// retrieval, unseeded failure, forced failure via <c>FailWith</c>,
/// and cancellation propagation.
/// </summary>
public sealed class IExchangeRateProviderTests
{
    private static readonly CancellationToken NoCt = CancellationToken.None;
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;
    private static readonly DateOnly Today = new(2026, 5, 21);

    [Fact]
    public async Task FetchAsync_returns_seeded_snapshot()
    {
        var snapshot = RateSnapshot
            .Create(Usd, Today, [ExchangeRate.Create(Usd, Eur, 0.92m, Today).Value])
            .Value;

        var sut = new InMemoryExchangeRateProvider();
        sut.Seed(Usd, Today, snapshot);

        var actual = await sut.FetchAsync(Usd, Today, NoCt);

        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().BeSameAs(snapshot);
    }

    [Fact]
    public async Task FetchAsync_returns_failure_when_unseeded()
    {
        var sut = new InMemoryExchangeRateProvider();

        var actual = await sut.FetchAsync(Eur, Today, NoCt);

        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("PROVIDER_UNAVAILABLE");
    }

    [Fact]
    public async Task FetchAsync_returns_FailWith_when_set()
    {
        var sut = new InMemoryExchangeRateProvider
        {
            FailWith = new DomainError("PROVIDER_UNSUPPORTED_CURRENCY", "nope"),
        };
        var snapshot = RateSnapshot
            .Create(Usd, Today, [ExchangeRate.Create(Usd, Eur, 0.92m, Today).Value])
            .Value;
        sut.Seed(Usd, Today, snapshot);

        var actual = await sut.FetchAsync(Usd, Today, NoCt);

        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("PROVIDER_UNSUPPORTED_CURRENCY");
    }

    [Fact]
    public async Task FetchAsync_failure_code_is_PROVIDER_UNAVAILABLE_by_default()
    {
        var sut = new InMemoryExchangeRateProvider();

        var actual = await sut.FetchAsync(Usd, Today, NoCt);

        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("PROVIDER_UNAVAILABLE");
    }

    [Fact]
    public async Task FetchAsync_honours_cancellation_token()
    {
        var sut = new InMemoryExchangeRateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.FetchAsync(Usd, Today, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
