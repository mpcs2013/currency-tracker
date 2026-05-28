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
    // Explicit backing field for the Rates navigation. EF Core's owned-type
    // materialisation populates this field directly (see the
    // `Navigation(s => s.Rates).UsePropertyAccessMode(PropertyAccessMode.Field)`
    // call in RateSnapshotConfiguration). Declared as `List<ExchangeRate>`
    // (not `IReadOnlyList<ExchangeRate>`) so EF Core can call `Add` on it
    // during materialisation; the public property exposes the read-only
    // view.
    private readonly List<ExchangeRate> _rates;

    /// <summary>Gets the base currency shared by every rate in the snapshot.</summary>
    public CurrencyCode Base { get; }

    /// <summary>Gets the observation date shared by every rate in the snapshot.</summary>
    public DateOnly AsOf { get; }

    /// <summary>Gets the rates in the snapshot.</summary>
    public IReadOnlyList<ExchangeRate> Rates => _rates;

    // Domain constructor — used by Create() to construct a fully-formed,
    // invariant-validated snapshot. Takes the rates collection because
    // the domain enforces "a snapshot is constructed atomically with its
    // rates"; you can't have a half-built snapshot.
    private RateSnapshot(CurrencyCode @base, DateOnly asOf, List<ExchangeRate> rates)
    {
        Base = @base;
        AsOf = asOf;
        _rates = rates;
    }

    // EF Core materialisation constructor — takes ONLY the scalar mapped
    // properties (Base, AsOf). EF Core constructs the snapshot first, then
    // populates _rates by writing to the backing field directly via the
    // field-access-mode configuration in RateSnapshotConfiguration.
    // EF Core's constructor binder rejects navigation parameters
    // ("Navigations to related entities, including references to owned
    // types, cannot be bound"), which is why the domain constructor above
    // can't double as the EF constructor.
    private RateSnapshot(CurrencyCode @base, DateOnly asOf)
    {
        Base = @base;
        AsOf = asOf;
        _rates = new List<ExchangeRate>();
    }

    /// <summary>Creates a validated <see cref="RateSnapshot"/>.</summary>
    public static Result<RateSnapshot> Create(
        CurrencyCode @base,
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
            if (rate.Base != @base)
            {
                return Result<RateSnapshot>.Failure(
                    DomainError.Validation(
                        "SNAPSHOT_BASE_MISMATCH",
                        $"Rate for {rate.Quote.Value} has base {rate.Base.Value}, expected {@base.Value}."
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

        return Result<RateSnapshot>.Success(new RateSnapshot(@base, asOf, materialised));
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

    /// <inheritdoc/>
    public bool Equals(RateSnapshot? other) =>
        other is not null && Base == other.Base && AsOf == other.AsOf;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Base, AsOf);
}
