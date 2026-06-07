using CurrencyTracker.Application.Messaging;
using FluentValidation.TestHelper;

namespace CurrencyTracker.Application.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="GetLatestRatesQueryValidator"/>.
/// </summary>
public sealed class GetLatestRatesQueryValidatorTests
{
    [Fact]
    public void Valid_Uppercase_three_letter_base_currency_passes()
    {
        var validator = new GetLatestRatesQueryValidator();
        var query = new GetLatestRatesQuery("USD");

        var result = validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Base);
    }

    [Theory]
    [InlineData("")]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U$D")]
    public void Invalid_base_currency_fail(string code)
    {
        var validator = new GetLatestRatesQueryValidator();
        var query = new GetLatestRatesQuery(code);

        var result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Base);
    }
}
