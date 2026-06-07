using CurrencyTracker.Domain.Rates;
using FluentValidation;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Query requesting the most recent exchange-rate snapshot for a base
/// currency.
/// </summary>
/// <param name="BaseCurrency">Three-letter uppercase ISO 4217 base currency code.</param>
public sealed record GetLatestRatesQuery(string BaseCurrency);

/// <summary>
/// Application-layer read model for one exchange-rate observation.
/// </summary>
/// <param name="BaseCurrency">Three-letter base currency code.</param>
/// <param name="QuoteCurrency">Three-letter quote currency code.</param>
/// <param name="Rate">Observed numeric exchange-rate value.</param>
/// <param name="AsOf">Business date of the observation.</param>
public sealed record ExchangeRateDto(
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    DateOnly AsOf
)
{
    /// <summary>
    /// Projects a domain <see cref="RateSnapshot"/> into Application read-model
    /// records suitable for API responses and cache serialisation.
    /// </summary>
    /// <param name="snapshot">Snapshot to project.</param>
    /// <returns>Projected DTOs, one per quote currency in the snapshot.</returns>
    public static IReadOnlyList<ExchangeRateDto> FromSnapshot(RateSnapshot snapshot)
    {
        var projected = new List<ExchangeRateDto>(snapshot.Rates.Count);

        foreach (var rate in snapshot.Rates)
        {
            projected.Add(
                new ExchangeRateDto(
                    BaseCurrency: snapshot.Base.Value,
                    QuoteCurrency: rate.Quote.Value,
                    Rate: rate.Rate,
                    AsOf: snapshot.AsOf
                )
            );
        }

        return projected;
    }
}

/// <summary>
/// Validator for <see cref="GetLatestRatesQuery"/>.
/// </summary>
public sealed class GetLatestRatesQueryValidator : AbstractValidator<GetLatestRatesQuery>
{
    /// <summary>
    /// Configures validation for the base currency code.
    /// </summary>
    public GetLatestRatesQueryValidator()
    {
        RuleFor(x => x.BaseCurrency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("BaseCurrency is required.")
            .Matches("^[A-Z]{3}$")
            .WithMessage("BaseCurrency must be a 3-letter uppercase ISO 4217 code.");
    }
}
