using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.UnitTests.Currencies;

/// <summary>
/// Tests covering <see cref="Currency"/> creation, rename validation, and
/// identity-based equality semantics.
/// </summary>
public sealed class CurrencyTests
{
    [Fact]
    public void Create_valid_values_returns_success()
    {
        var code = CurrencyCode.Create("USD").Value;

        var result = Currency.Create(code, "US Dollar", 840);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code);
        result.Value.Name.Should().Be("US Dollar");
        result.Value.NumericCode.Should().Be(840);
    }

    [Fact]
    public void Create_empty_name_returns_failure_with_CURRENCY_NAME_REQUIRED()
    {
        var code = CurrencyCode.Create("USD").Value;

        var result = Currency.Create(code, "", 840);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CURRENCY_NAME_REQUIRED");
    }

    [Fact]
    public void Create_numeric_code_out_of_range_returns_failure_with_CURRENCY_NUMERIC_CODE_RANGE()
    {
        var code = CurrencyCode.Create("USD").Value;

        var result = Currency.Create(code, "US Dollar", 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CURRENCY_NUMERIC_CODE_RANGE");
    }

    [Fact]
    public void Rename_valid_name_updates_name()
    {
        var currency = Currency.Create(CurrencyCode.Create("USD").Value, "US Dollar", 840).Value;

        var result = currency.Rename("United States Dollar");

        result.IsSuccess.Should().BeTrue();
        currency.Name.Should().Be("United States Dollar");
    }

    [Fact]
    public void Rename_empty_name_returns_failure_with_CURRENCY_NAME_REQUIRED()
    {
        var currency = Currency.Create(CurrencyCode.Create("USD").Value, "US Dollar", 840).Value;

        var result = currency.Rename("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CURRENCY_NAME_REQUIRED");
    }

    [Fact]
    public void Two_entities_with_same_code_and_different_names_are_equal()
    {
        var code = CurrencyCode.Create("USD").Value;
        var a = Currency.Create(code, "US Dollar", 840).Value;
        var b = Currency.Create(code, "United States Dollar", 840).Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Two_entities_with_different_codes_are_not_equal()
    {
        var a = Currency.Create(CurrencyCode.Create("USD").Value, "US Dollar", 840).Value;
        var b = Currency.Create(CurrencyCode.Create("EUR").Value, "Euro", 978).Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_entities_have_the_same_hash_code()
    {
        var code = CurrencyCode.Create("USD").Value;
        var a = Currency.Create(code, "US Dollar", 840).Value;
        var b = Currency.Create(code, "United States Dollar", 840).Value;

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
