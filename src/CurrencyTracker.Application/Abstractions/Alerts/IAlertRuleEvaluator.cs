using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.Abstractions.Alerts;

/// <summary>
/// Evaluates every enabled <see cref="AlertRule"/> for a base currency
/// against the rate movement between the previous day's snapshot and the
/// observation day's snapshot, returning one constructed
/// <see cref="Alert"/> per rule that fired.
/// </summary>
/// <remarks>
/// The adapter fetches and constructs; the domain decides.
/// <c>AlertRule.ShouldTrigger</c> owns the threshold comparison and
/// <c>Alert.Create</c> owns the invariants — implementations must not
/// duplicate either. When either snapshot is missing, the answer is an
/// empty list, not an exception: "nothing to compare" is a normal
/// state on the first ingestion day.
/// </remarks>
public interface IAlertRuleEvaluator
{
    /// <summary>
    /// Evaluates the enabled rules for <paramref name="baseCurrency"/>
    /// against the movement from the day before <paramref name="asOf"/>
    /// to <paramref name="asOf"/>.
    /// </summary>
    /// <param name="baseCurrency">The base currency whose rules to evaluate.</param>
    /// <param name="asOf">The observation date of the newly ingested snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel the underlying I/O.</param>
    /// <returns>The alerts that fired; empty when no rule fired or a snapshot is missing.</returns>
    Task<IReadOnlyList<Alert>> EvaluateAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    );
}
