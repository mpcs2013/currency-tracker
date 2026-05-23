using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.Rates;

/// <summary>
/// Exchange-rate observation for a currency pair on a specific date. The
/// entity identity is the composite key (<see cref="Base"/>,
/// <see cref="Quote"/>, <see cref="AsOf"/>).
/// </summary>
public sealed record ExchangeRate
{
    /// <summary>Gets the base currency of the pair.</summary>
    public CurrencyCode Base { get; }

    /// <summary>Gets the quote currency of the pair.</summary>
    public CurrencyCode Quote { get; }

    /// <summary>Gets the numeric exchange rate value.</summary>
    public decimal Rate { get; }

    /// <summary>Gets the observation date for this rate.</summary>
    public DateOnly AsOf { get; }

    private ExchangeRate(CurrencyCode @base, CurrencyCode quote, decimal rate, DateOnly asOf)
    {
        Base = @base;
        Quote = quote;
        Rate = rate;
        AsOf = asOf;
    }

    /// <summary>
    /// Creates an <see cref="ExchangeRate"/> after validating pair, rate,
    /// and observation date.
    /// </summary>
    /// <param name="base">Base currency of the pair.</param>
    /// <param name="quote">Quote currency of the pair.</param>
    /// <param name="rate">Observed rate value; must be greater than zero.</param>
    /// <param name="asOf">Observation date; must not be <see langword="default"/>.</param>
    /// <returns>A success carrying the entity, or a validation failure.</returns>
    public static Result<ExchangeRate> Create(
        CurrencyCode @base,
        CurrencyCode quote,
        decimal rate,
        DateOnly asOf
    )
    {
        if (@base == quote)
        {
            return Result<ExchangeRate>.Failure(
                DomainError.Validation(
                    "RATE_SAME_CURRENCY", 
                    "Base and quote currencies must differ."
                )
            );
        }

        if (rate <= 0)
        {
            return Result<ExchangeRate>.Failure(
                DomainError.Validation(
                    "RATE_NONPOSITIVE", 
                    "Rate must be strictly positive."
                )
            );
        }

        if (asOf == default)
        {
            return Result<ExchangeRate>.Failure(
                DomainError.Validation(
                    "RATE_ASOF_REQUIRED", 
                    "AsOf date is required."
                )
            );
        }

        return Result<ExchangeRate>.Success(new ExchangeRate(@base, quote, rate, asOf));
    }

    /// <summary>
    /// Compares identity equality using only <see cref="Base"/>,
    /// <see cref="Quote"/>, and <see cref="AsOf"/>.
    /// </summary>
    /// <param name="other">Other entity to compare.</param>
    /// <returns><see langword="true"/> when composite identities match.</returns>
    public bool Equals(ExchangeRate? other) =>
        other is not null && Base == other.Base && Quote == other.Quote && AsOf == other.AsOf;

    /// <summary>
    /// Returns a hash code derived from the composite identity only.
    /// </summary>
    /// <returns>Identity hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(Base, Quote, AsOf);
}
