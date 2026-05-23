using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.UnitTests.Currencies;

/// <summary>
/// Tests covering <see cref="CurrencyCode"/>'s validation pipeline,
/// equality semantics, and <c>ToString</c> formatting.
/// </summary>
public sealed class CurrencyCodeTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    public void Create_known_three_letter_code_succeeds(string raw)
    {
        var result = CurrencyCode.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(raw);
    }

    [Fact]
    public void Create_null_returns_failure_with_CURRENCY_CODE_REQUIRED()
    {
        var result = CurrencyCode.Create(null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CURRENCY_CODE_REQUIRED");
    }

    [Fact]
    public void Create_empty_returns_failure_with_CURRENCY_CODE_REQUIRED()
    {
        var result = CurrencyCode.Create("");

        result.Error.Code.Should().Be("CURRENCY_CODE_REQUIRED");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    public void Create_wrong_length_returns_failure_with_CURRENCY_CODE_LENGTH(string raw)
    {
        var result = CurrencyCode.Create(raw);

        result.Error.Code.Should().Be("CURRENCY_CODE_LENGTH");
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("US1")]
    [InlineData("U$D")]
    public void Create_non_uppercase_letters_returns_failure_with_CURRENCY_CODE_FORMAT(string raw)
    {
        var result = CurrencyCode.Create(raw);

        result.Error.Code.Should().Be("CURRENCY_CODE_FORMAT");
    }

    [Fact]
    public void Create_unknown_three_uppercase_letters_returns_failure_with_CURRENCY_CODE_UNKNOWN()
    {
        var result = CurrencyCode.Create("XYZ");

        result.Error.Code.Should().Be("CURRENCY_CODE_UNKNOWN");
    }

    [Fact]
    public void ToString_returns_the_underlying_value()
    {
        var sut = CurrencyCode.Create("USD").Value;

        sut.ToString().Should().Be("USD");
    }

    [Fact]
    public void Two_codes_with_the_same_value_are_equal()
    {
        var a = CurrencyCode.Create("USD").Value;
        var b = CurrencyCode.Create("USD").Value;

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Two_codes_with_different_values_are_not_equal()
    {
        var a = CurrencyCode.Create("USD").Value;
        var b = CurrencyCode.Create("EUR").Value;

        a.Should().NotBe(b);
    }
}
