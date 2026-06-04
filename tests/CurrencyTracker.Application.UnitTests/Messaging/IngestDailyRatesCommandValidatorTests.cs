using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Application.UnitTests.Fakes;
using FluentValidation.TestHelper;

namespace CurrencyTracker.Application.UnitTests.Messaging;

public sealed class IngestDailyRatesCommandValidatorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 06, 04, 12, 00, 00, TimeSpan.Zero);

    [Fact]
    public void Valid_command_passes_validation()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("USD", new DateOnly(2026, 06, 04));

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_base_currency_fails_validation()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand(string.Empty, new DateOnly(2026, 06, 04));

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Unknown_base_currency_fails_validation()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("ZZZ", new DateOnly(2026, 06, 04));

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Default_as_of_fails_validation()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("USD", default);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AsOf);
    }

    [Fact]
    public void Future_as_of_fails_validation()
    {
        var validator = CreateValidator();
        var command = new IngestDailyRatesCommand("USD", new DateOnly(2026, 06, 05));

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AsOf);
    }

    private static IngestDailyRatesCommandValidator CreateValidator() =>
        new(new FixedDateTimeProvider(FixedNow));
}
