using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Rates;

namespace CurrencyTracker.Domain.UnitTests.Rates;

/// <summary>
/// Tests covering <see cref="ExchangeRate"/> creation validation and
/// identity-based equality semantics.
/// </summary>
public sealed class ExchangeRateTests
{
    [Fact]
    public void Create_valid_values_returns_success()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(@base, quote, 0.92m, new DateOnly(2026, 5, 23));

        result.IsSuccess.Should().BeTrue();
        result.Value.Base.Should().Be(@base);
        result.Value.Quote.Should().Be(quote);
        result.Value.Rate.Should().Be(0.92m);
        result.Value.AsOf.Should().Be(new DateOnly(2026, 5, 23));
    }

    [Fact]
    public void Create_same_base_and_quote_returns_failure_with_EXCHANGE_RATE_BASE_QUOTE_MUST_DIFFER()
    {
        var code = CurrencyCode.Create("USD").Value;

        var result = ExchangeRate.Create(code, code, 1.00m, new DateOnly(2026, 5, 23));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EXCHANGE_RATE_BASE_QUOTE_MUST_DIFFER");
    }

    [Fact]
    public void Create_zero_rate_returns_failure_with_EXCHANGE_RATE_RATE_POSITIVE()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(@base, quote, 0m, new DateOnly(2026, 5, 23));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EXCHANGE_RATE_RATE_POSITIVE");
    }

    [Fact]
    public void Create_negative_rate_returns_failure_with_EXCHANGE_RATE_RATE_POSITIVE()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(@base, quote, -0.01m, new DateOnly(2026, 5, 23));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EXCHANGE_RATE_RATE_POSITIVE");
    }

    [Fact]
    public void Create_default_as_of_returns_failure_with_EXCHANGE_RATE_AS_OF_REQUIRED()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;

        var result = ExchangeRate.Create(@base, quote, 0.92m, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EXCHANGE_RATE_AS_OF_REQUIRED");
    }

    [Fact]
    public void Two_entities_with_same_identity_and_different_rate_are_equal()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;
        var asOf = new DateOnly(2026, 5, 23);
        var a = ExchangeRate.Create(@base, quote, 0.92m, asOf).Value;
        var b = ExchangeRate.Create(@base, quote, 0.93m, asOf).Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Two_entities_with_different_as_of_are_not_equal()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;
        var a = ExchangeRate.Create(@base, quote, 0.92m, new DateOnly(2026, 5, 23)).Value;
        var b = ExchangeRate.Create(@base, quote, 0.92m, new DateOnly(2026, 5, 24)).Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_entities_have_the_same_hash_code()
    {
        var @base = CurrencyCode.Create("USD").Value;
        var quote = CurrencyCode.Create("EUR").Value;
        var asOf = new DateOnly(2026, 5, 23);
        var a = ExchangeRate.Create(@base, quote, 0.92m, asOf).Value;
        var b = ExchangeRate.Create(@base, quote, 0.93m, asOf).Value;

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
