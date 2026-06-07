using CurrencyTracker.Domain.Rates;
using FluentValidation;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Query requesting the most recent exchange-rate snapshot for a base
/// currency.
/// </summary>
/// <param name="Base">Three-letter uppercase ISO 4217 base currency code.</param>
public sealed record GetLatestRatesQuery(string Base);

/// <summary>
/// Application-layer read model for one exchange-rate observation.
/// </summary>
/// <param name="Base">Three-letter base currency code.</param>
/// <param name="Quote">Three-letter quote currency code.</param>
/// <param name="Rate">Observed numeric exchange-rate value.</param>
/// <param name="AsOf">Business date of the observation.</param>
public sealed record ExchangeRateDto(string Base, string Quote, decimal Rate, DateOnly AsOf)
{
    /// <summary>
    /// Projects a domain <see cref="RateSnapshot"/> into Application read-model
    /// records suitable for API responses and cache serialisation.
    /// </summary>
    /// <param name="snapshot">Snapshot to project.</param>
    /// <returns>Projected DTOs, one per quote currency in the snapshot.</returns>
    public static IReadOnlyList<ExchangeRateDto> FromSnapshot(RateSnapshot snapshot)
    {
        return snapshot
            .Rates.Select(r => new ExchangeRateDto(
                r.Base.Value,
                r.Quote.Value,
                r.Rate,
                snapshot.AsOf
            ))
            .ToList();
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
        RuleFor(x => x.Base)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Base is required.")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Base must be a 3-letter uppercase ISO 4217 code.");
    }
}
