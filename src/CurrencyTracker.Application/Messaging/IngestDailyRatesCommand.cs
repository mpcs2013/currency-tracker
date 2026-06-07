using CurrencyTracker.Application.Abstractions.Time;
using CurrencyTracker.Domain.Currencies;
using FluentValidation;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Message describing which daily rates snapshot should be ingested.
/// </summary>
/// <param name="BaseCurrency">ISO 4217 base currency code.</param>
/// <param name="AsOf">The business date for the snapshot.</param>
public sealed record IngestDailyRatesCommand(string BaseCurrency, DateOnly AsOf);

/// <summary>
/// Validator for <see cref="IngestDailyRatesCommand"/>.
/// </summary>
public sealed class IngestDailyRatesCommandValidator : AbstractValidator<IngestDailyRatesCommand>
{
    /// <summary>
    /// Initializes validation rules for ingestion requests.
    /// </summary>
    public IngestDailyRatesCommandValidator(IDateTimeProvider clock)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        RuleFor(x => x.BaseCurrency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Base is required.")
            .Must(raw => CurrencyCode.Create(raw).IsSuccess)
            .WithMessage("Base must be a known currency ISO 4217 code.");

        RuleFor(x => x.AsOf)
            .Must(asOf => asOf != default)
            .WithMessage("AsOf date is required.")
            .Must(asOf => asOf <= today)
            .WithMessage("AsOf date cannot be in the future.");
    }
}
