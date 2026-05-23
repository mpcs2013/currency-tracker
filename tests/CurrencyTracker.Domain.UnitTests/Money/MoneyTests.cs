using CurrencyTracker.Domain.Currencies;
using CurrencyTracker.Domain.Exceptions;

namespace CurrencyTracker.Domain.UnitTests.Money;

/// <summary>
/// Tests covering <see cref="Money"/> construction, the same-currency
/// invariant on <c>+</c> and <c>-</c>, scalar multiplication, structural
/// equality, and the domain-specific <see cref="Money"/> format.
/// </summary>
public sealed class MoneyTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.Create("USD").Value;
    private static readonly CurrencyCode Eur = CurrencyCode.Create("EUR").Value;

    [Fact]
    public void Construction_assigns_amount_and_currency()
    {
        var sut = new Domain.Money.Money(100m, Usd);

        sut.Amount.Should().Be(100m);
        sut.Currency.Should().Be(Usd);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(100)]
    public void Construction_accepts_any_decimal_amount(decimal amount)
    {
        // Money does not reject negative or zero amounts — debits, losses,
        // and zero balances are all legitimate domain values.
        var sut = new Domain.Money.Money(amount, Usd);

        sut.Amount.Should().Be(amount);
    }

    [Fact]
    public void Addition_same_currency_returns_sum_in_that_currency()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(50m, Usd);

        var result = a + b;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Usd);
    }

    [Fact]
    public void Addition_different_currencies_throws_CurrencyMismatchException()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(50m, Eur);

        var act = () => _ = a + b;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void Subtraction_same_currency_returns_difference_in_that_currency()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(30m, Usd);

        var result = a - b;

        result.Amount.Should().Be(70m);
        result.Currency.Should().Be(Usd);
    }

    [Fact]
    public void Subtraction_can_produce_negative_amount()
    {
        var a = new Domain.Money.Money(30m, Usd);
        var b = new Domain.Money.Money(100m, Usd);

        var result = a - b;

        result.Amount.Should().Be(-70m);
    }

    [Fact]
    public void Subtraction_different_currencies_throws_CurrencyMismatchException()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(50m, Eur);

        var act = () => _ = a - b;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Theory]
    [InlineData(2, 200)]
    [InlineData(0.5, 50)]
    [InlineData(0, 0)]
    [InlineData(-1, -100)]
    public void Multiplication_by_scalar_scales_amount_and_preserves_currency(
        decimal scalar,
        decimal expected
    )
    {
        var sut = new Domain.Money.Money(100m, Usd);

        var result = sut * scalar;

        result.Amount.Should().Be(expected);
        result.Currency.Should().Be(Usd);
    }

    [Fact]
    public void Two_monies_with_same_amount_and_currency_are_equal()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(100m, Usd);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Two_monies_with_same_amount_but_different_currencies_are_not_equal()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(100m, Eur);

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Two_monies_with_different_amounts_in_same_currency_are_not_equal()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(50m, Usd);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equal_monies_have_the_same_hash_code()
    {
        var a = new Domain.Money.Money(100m, Usd);
        var b = new Domain.Money.Money(100m, Usd);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_returns_amount_then_currency_code()
    {
        var sut = new Domain.Money.Money(99.95m, Usd);

        sut.ToString().Should().Be("99.95 USD");
    }
}
