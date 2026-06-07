using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using FluentValidation.TestHelper;

namespace CurrencyTracker.Application.UnitTests.Messaging;

public sealed class IngestDailyRatesCommandValidatorTests
{
    private static readonly DateOnly Today = new(2026, 05, 28);

    [Fact]
    public void Valid_command_passes()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("USD", Today);

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_base_currency_fails()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand(string.Empty, Today);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Base);
    }

    [Fact]
    public void Unknown_base_currency_fails()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("ZZZ", Today);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Base);
    }

    [Fact]
    public void Default_as_of_fails()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("USD", default);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AsOf);
    }

    [Fact]
    public void Future_as_of_fails()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("USD", Today.AddDays(1));

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AsOf);
    }

    private static IngestDailyRatesCommandValidator CreateValidator() =>
        new(new FixedDateTimeProvider(new DateTimeOffset(Today, TimeOnly.MinValue, TimeSpan.Zero)));
}
