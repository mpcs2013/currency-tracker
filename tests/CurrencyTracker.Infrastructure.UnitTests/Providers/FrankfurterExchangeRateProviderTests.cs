using System.Net;
using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CurrencyTracker.Infrastructure.UnitTests.Providers;

/// <summary>
/// Unit tests for <see cref="FrankfurterExchangeRateProvider"/>'s
/// anti-corruption behaviour: wire-DTO-to-domain mapping, unknown-quote
/// skipping, and HTTP-failure-to-<c>Result.Failure</c> translation.
/// </summary>
public sealed class FrankfurterExchangeRateProviderTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly DateOnly AsOf = new(2026, 5, 28);

    private static FrankfurterExchangeRateProvider BuildSut(HttpStatusCode status, string body)
    {
        var handler = new StubHttpMessageHandler(status, body);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
        var client = new FrankfurterClient(httpClient);

        return new FrankfurterExchangeRateProvider(
            client,
            NullLogger<FrankfurterExchangeRateProvider>.Instance
        );
    }

    [Fact]
    public async Task FetchAsync_maps_known_quotes_to_snapshot()
    {
        var sut = BuildSut(
            HttpStatusCode.OK,
            """{"amount":1.0,"base":"USD","date":"2026-05-28","rates":{"EUR":0.92,"GBP":0.79}}"""
        );

        var result = await sut.FetchAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Base.Should().Be(Usd);
        result.Value.AsOf.Should().Be(AsOf);
        result.Value.Rates.Should().HaveCount(2);
    }

    [Fact]
    public async Task FetchAsync_skips_unknown_quote_currencies()
    {
        var sut = BuildSut(
            HttpStatusCode.OK,
            """{"amount":1.0,"base":"USD","date":"2026-05-28","rates":{"EUR":0.92,"THB":36.1}}"""
        );

        var result = await sut.FetchAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rates.Should().ContainSingle(r => r.Quote == CurrencyCode.Create("EUR").Value);
    }

    [Fact]
    public async Task FetchAsync_returns_unsupported_currency_on_4xx()
    {
        var sut = BuildSut(HttpStatusCode.NotFound, """{"message":"not found"}""");

        var result = await sut.FetchAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROVIDER_UNSUPPORTED_CURRENCY");
    }

    [Fact]
    public async Task FetchAsync_returns_unavailable_on_5xx()
    {
        var sut = BuildSut(HttpStatusCode.InternalServerError, "boom");

        var result = await sut.FetchAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROVIDER_UNAVAILABLE");
    }

    [Fact]
    public async Task FetchAsync_returns_unavailable_when_all_quotes_unknown()
    {
        var sut = BuildSut(
            HttpStatusCode.OK,
            """{"amount":1.0,"base":"USD","date":"2026-05-28","rates":{"THB":36.1,"PLN":4.0}}"""
        );

        var result = await sut.FetchAsync(Usd, AsOf, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("SNAPSHOT_EMPTY");
    }
}
