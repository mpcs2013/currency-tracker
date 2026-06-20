using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.AspNetCore.Mvc; // [property: FromQuery] on the record

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Query for a base/quote rate over a bounded date range. No marker
/// interface (Decision 0003). Bound from the query string in the endpoint.
/// The range parameters are named <c>FromInclusive</c>/<c>ToInclusive</c>
/// (not <c>From</c>/<c>To</c>) because <c>To</c> collides with a reserved
/// keyword on an externally-visible member (CA1716); the <c>?from=</c>/
/// <c>?to=</c> query keys are preserved via the binding attributes.
/// </summary>
/// <param name="Base">Base currency code.</param>
/// <param name="Quote">Quote currency code.</param>
/// <param name="From">Inclusive range start (bound from <c>?from=</c>).</param>
/// <param name="To">Inclusive range end (bound from <c>?to=</c>).</param>
public sealed record GetRateHistoryQuery(
    string Base,
    string Quote,
    [property: SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Public query-string binding key for GET /api/v1/rates/history; "
            + "Wolverine binds by member name and ignores [FromQuery(Name)] (#641), so the "
            + "member must be named 'From' to bind ?from=."
    )]
        DateOnly From,
    [property: SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Public query-string binding key for GET /api/v1/rates/history; "
            + "Wolverine binds by member name and ignores [FromQuery(Name)] (#641), so the "
            + "member must be named 'To' to bind ?to=."
    )]
        DateOnly To
);

/// <summary>A single dated rate point in a history response.</summary>
/// <param name="AsOf">Observation date.</param>
/// <param name="Rate">The base→quote rate on that date.</param>
public sealed record RateHistoryPointDto(DateOnly AsOf, decimal Rate);

/// <summary>
/// Validates <see cref="GetRateHistoryQuery"/>: well-formed distinct codes
/// and a non-empty, bounded date range. Runs in the FluentValidation
/// middleware (Phase 6.3).
/// </summary>
public sealed class GetRateHistoryQueryValidator : AbstractValidator<GetRateHistoryQuery>
{
    private const int MaxRangeDays = 366;

    /// <summary>Initialises the validation rules.</summary>
    public GetRateHistoryQueryValidator()
    {
        RuleFor(q => q.Base).NotEmpty().Matches("^[A-Z]{3}$");
        RuleFor(q => q.Quote).NotEmpty().Matches("^[A-Z]{3}$");
        RuleFor(q => q).Must(q => q.Base != q.Quote).WithMessage("Base and Quote must differ.");
        RuleFor(q => q.From)
            .LessThanOrEqualTo(q => q.To)
            .WithMessage("From must be on or before To.");
        RuleFor(q => q)
            .Must(q => q.To.DayNumber - q.From.DayNumber <= MaxRangeDays)
            .WithMessage($"Date range must not exceed {MaxRangeDays} days.");
    }
}
