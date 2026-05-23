using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Domain.UnitTests.Rates;

/// <summary>
/// Tests covering <see cref="ExchangeRate"/> creation validation and
/// identity-based equality semantics on the
/// (<c>Base</c>, <c>Quote</c>, <c>AsOf</c>) triple.
/// </summary>
public sealed class ExchangeRateTests
{
    private static readonly DateOnly TestAsOf = new(2026, 5, 23);

    [Fact]
    public void Create_valid_values_returns_success()
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteCurrency = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(baseCurrency, quoteCurrency, 0.92m, TestAsOf);

        result.IsSuccess.Should().BeTrue();
        result.Value.Base.Should().Be(baseCurrency);
        result.Value.Quote.Should().Be(quoteCurrency);
        result.Value.Rate.Should().Be(0.92m);
        result.Value.AsOf.Should().Be(TestAsOf);
    }

    [Fact]
    public void Create_same_base_and_quote_returns_failure_with_RATE_SAME_CURRENCY()
    {
        var code = CurrencyCode.Create("USD").Value;

        var result = ExchangeRate.Create(code, code, 1.00m, TestAsOf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RATE_SAME_CURRENCY");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Create_non_positive_rate_returns_failure_with_RATE_NONPOSITIVE(decimal rate)
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteCurrency = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(baseCurrency, quoteCurrency, rate, TestAsOf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RATE_NONPOSITIVE");
    }

    [Fact]
    public void Create_default_as_of_returns_failure_with_RATE_ASOF_REQUIRED()
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteCurrency = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(baseCurrency, quoteCurrency, 0.92m, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RATE_ASOF_REQUIRED");
    }

    [Fact]
    public void Two_entities_with_same_identity_and_different_rate_are_equal()
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteCurrency = CurrencyCode.Create("EUR").Value;

        var a = ExchangeRate.Create(baseCurrency, quoteCurrency, 0.92m, TestAsOf).Value;
        var b = ExchangeRate.Create(baseCurrency, quoteCurrency, 0.93m, TestAsOf).Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Two_entities_with_different_as_of_are_not_equal()
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteCurrency = CurrencyCode.Create("EUR").Value;

        var a = ExchangeRate
            .Create(baseCurrency, quoteCurrency, 0.92m, new DateOnly(2026, 5, 23))
            .Value;
        var b = ExchangeRate
            .Create(baseCurrency, quoteCurrency, 0.92m, new DateOnly(2026, 5, 24))
            .Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Two_entities_with_different_quote_are_not_equal()
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteEur = CurrencyCode.Create("EUR").Value;
        var quoteGbp = CurrencyCode.Create("GBP").Value;

        var a = ExchangeRate.Create(baseCurrency, quoteEur, 0.92m, TestAsOf).Value;
        var b = ExchangeRate.Create(baseCurrency, quoteGbp, 0.92m, TestAsOf).Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_entities_have_the_same_hash_code()
    {
        var baseCurrency = CurrencyCode.Create("USD").Value;
        var quoteCurrency = CurrencyCode.Create("EUR").Value;

        var a = ExchangeRate.Create(baseCurrency, quoteCurrency, 0.92m, TestAsOf).Value;
        var b = ExchangeRate.Create(baseCurrency, quoteCurrency, 0.93m, TestAsOf).Value;

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
