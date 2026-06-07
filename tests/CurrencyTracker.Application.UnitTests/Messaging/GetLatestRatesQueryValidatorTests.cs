using CurrencyTracker.Application.Messaging;
using FluentValidation.TestHelper;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="GetLatestRatesQueryValidator"/>.
/// </summary>
public sealed class GetLatestRatesQueryValidatorTests
{
    [Fact]
    public void Uppercase_three_letter_base_currency_passes_validation()
    {
        var validator = new GetLatestRatesQueryValidator();
        var query = new GetLatestRatesQuery("USD");

        var result = validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U$D")]
    public void Invalid_base_currency_formats_fail_validation(string baseCurrency)
    {
        var validator = new GetLatestRatesQueryValidator();
        var query = new GetLatestRatesQuery(baseCurrency);

        var result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }
}
