using CurrencyTracker.Application.Messaging;
using FluentValidation.TestHelper;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>Unit tests for <see cref="GetRateHistoryQueryValidator"/>.</summary>
public sealed class GetRateHistoryQueryValidatorTests
{
    private readonly GetRateHistoryQueryValidator _validator = new();

    private static readonly DateOnly From = new(2026, 5, 1);
    private static readonly DateOnly To = new(2026, 5, 28);

    [Fact]
    public void Valid_query_passes()
    {
        var result = _validator.TestValidate(new GetRateHistoryQuery("USD", "EUR", From, To));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Base_equal_to_quote_fails()
    {
        var result = _validator.TestValidate(new GetRateHistoryQuery("USD", "USD", From, To));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void From_after_to_fails()
    {
        // FromInclusive (To) is after ToInclusive (From).
        var result = _validator.TestValidate(new GetRateHistoryQuery("USD", "EUR", To, From));

        result.ShouldHaveValidationErrorFor(q => q.From);
    }

    [Fact]
    public void Range_over_366_days_fails()
    {
        var result = _validator.TestValidate(
            new GetRateHistoryQuery("USD", "EUR", From, From.AddDays(400))
        );

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("usd", "EUR")]
    [InlineData("US", "EUR")]
    [InlineData("USD", "eur")]
    [InlineData("USD", "EURO")]
    public void Malformed_codes_fail(string @base, string quote)
    {
        var result = _validator.TestValidate(new GetRateHistoryQuery(@base, quote, From, To));

        result.IsValid.Should().BeFalse();
    }
}
