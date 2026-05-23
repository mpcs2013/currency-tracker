using CurrencyTracker.Domain.Common;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Domain.Rates;

/// <summary>
/// Aggregate root: an immutable collection of <see cref="ExchangeRate"/>
/// observations sharing a single <see cref="Base"/> and <see cref="AsOf"/>.
/// Invariant: every contained rate has the same <c>Base</c> and <c>AsOf</c>
/// as this snapshot; no two rates share a <c>Quote</c>.
/// </summary>
public sealed record RateSnapshot
{
    /// <summary>Gets the base currency shared by every rate in the snapshot.</summary>
    public CurrencyCode Base { get; }

    /// <summary>Gets the observation date shared by every rate in the snapshot.</summary>
    public DateOnly AsOf { get; }

    /// <summary>Gets the rates in the snapshot.</summary>
    public IReadOnlyList<ExchangeRate> Rates { get; }

    private RateSnapshot(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        IReadOnlyList<ExchangeRate> rates
    )
    {
        Base = baseCurrency;
        AsOf = asOf;
        Rates = rates;
    }

    /// <summary>Creates a validated <see cref="RateSnapshot"/>.</summary>
    public static Result<RateSnapshot> Create(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        IEnumerable<ExchangeRate> rates
    )
    {
        var materialised = rates.ToList();

        if (materialised.Count == 0)
        {
            return Result<RateSnapshot>.Failure(
                DomainError.Validation(
                    "SNAPSHOT_EMPTY",
                    "A snapshot must contain at least one rate."
                )
            );
        }

        foreach (var rate in materialised)
        {
            if (rate.Base != baseCurrency)
            {
                return Result<RateSnapshot>.Failure(
                    DomainError.Validation(
                        "SNAPSHOT_BASE_MISMATCH",
                        $"Rate for {rate.Quote.Value} has base {rate.Base.Value}, expected {baseCurrency.Value}."
                    )
                );
            }

            if (rate.AsOf != asOf)
            {
                return Result<RateSnapshot>.Failure(
                    DomainError.Validation(
                        "SNAPSHOT_ASOF_MISMATCH",
                        $"Rate for {rate.Quote.Value} is dated {rate.AsOf:yyyy-MM-dd}, expected {asOf:yyyy-MM-dd}."
                    )
                );
            }
        }

        var distinctQuotes = new HashSet<CurrencyCode>();
        foreach (var rate in materialised)
        {
            if (!distinctQuotes.Add(rate.Quote))
            {
                return Result<RateSnapshot>.Failure(
                    DomainError.Validation(
                        "SNAPSHOT_DUPLICATE_QUOTE",
                        $"Duplicate rate for quote {rate.Quote.Value}."
                    )
                );
            }
        }

        return Result<RateSnapshot>.Success(
            new RateSnapshot(baseCurrency, asOf, materialised.AsReadOnly())
        );
    }

    /// <summary>
    /// Attempts to find the rate for the given quote currency. Returns
    /// <see langword="true"/> if found; <paramref name="rate"/> is the
    /// matching rate. Returns <see langword="false"/> otherwise;
    /// <paramref name="rate"/> is undefined.
    /// </summary>
    public bool TryGetRate(CurrencyCode quote, out ExchangeRate rate)
    {
        foreach (var candidate in Rates)
        {
            if (candidate.Quote == quote)
            {
                rate = candidate;
                return true;
            }
        }
        rate = default!;
        return false;
    }

    /// <summary>
    /// Determines whether the specified RateSnapshot is equal to the current instance.
    /// </summary>
    /// <param name="other">The RateSnapshot to compare with the current instance.</param>
    /// <returns>true if the specified RateSnapshot is equal to the current instance; otherwise, false.</returns>
    public bool Equals(RateSnapshot? other) =>
        other is not null && Base == other.Base && AsOf == other.AsOf;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Base, AsOf);
}
