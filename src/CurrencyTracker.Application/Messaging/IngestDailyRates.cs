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
        RuleFor(x => x.BaseCurrency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(raw => CurrencyCode.Create(raw).IsSuccess)
            .WithMessage("BaseCurrency must be a known currency code.");

        RuleFor(x => x.AsOf)
            .NotEqual(default(DateOnly))
            .WithMessage("AsOf is required.")
            .Must((_, asOf) => asOf <= DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            .WithMessage("AsOf cannot be in the future.");
    }
}
