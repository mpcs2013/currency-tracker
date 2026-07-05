using CurrencyTracker.Domain.Common;

namespace CurrencyTracker.Domain.Alerts;

/// <summary>
/// Immutable record of an <see cref="AlertRule"/> firing: which rule fired,
/// for which observation date, what rates triggered it, and when. An alert
/// is a fact — it does not change after creation. The pair
/// (<see cref="RuleId"/>, <see cref="AsOfDate"/>) is the alert's business
/// identity: a rule fires at most once per observation date, enforced by a
/// unique index (ADR 0012).
/// </summary>
public sealed record Alert
{
    /// <summary>Gets the alert's unique identity.</summary>
    public Guid Id { get; }

    /// <summary>Gets the identifier of the <see cref="AlertRule"/> that fired.</summary>
    public Guid RuleId { get; }

    /// <summary>
    /// Gets the observation date of the snapshot that triggered the rule —
    /// the second half of the alert's business identity.
    /// </summary>
    public DateOnly AsOfDate { get; }

    /// <summary>Gets the exchange rate observed before the transition.</summary>
    public decimal PreviousRate { get; }

    /// <summary>Gets the exchange rate observed after the transition.</summary>
    public decimal CurrentRate { get; }

    /// <summary>
    /// Gets the absolute percent change between <see cref="PreviousRate"/> and
    /// <see cref="CurrentRate"/>, computed as
    /// <c>Math.Abs((currentRate - previousRate) / previousRate) * 100</c>.
    /// Stored redundantly so queries can filter on the pre-computed value
    /// without a derived-column trick.
    /// </summary>
    public decimal ObservedChangePercent { get; }

    /// <summary>Gets the UTC instant at which the rule fired.</summary>
    public DateTimeOffset FiredAt { get; }

    private Alert(
        Guid id,
        Guid ruleId,
        DateOnly asOfDate,
        decimal previousRate,
        decimal currentRate,
        decimal observedChangePercent,
        DateTimeOffset firedAt
    )
    {
        Id = id;
        RuleId = ruleId;
        AsOfDate = asOfDate;
        PreviousRate = previousRate;
        CurrentRate = currentRate;
        ObservedChangePercent = observedChangePercent;
        FiredAt = firedAt;
    }

    /// <summary>Creates a validated <see cref="Alert"/> with a new identity.</summary>
    /// <param name="ruleId">The identifier of the rule that fired.</param>
    /// <param name="asOfDate">The observation date that triggered the rule. Must not be the default value.</param>
    /// <param name="previousRate">The rate observed before the transition. Must be strictly positive.</param>
    /// <param name="currentRate">The rate observed after the transition. Must not be negative.</param>
    /// <param name="firedAt">The instant at which the rule fired. Must not be the default value.</param>
    /// <returns>A success carrying the entity, or a validation failure.</returns>
    public static Result<Alert> Create(
        Guid ruleId,
        DateOnly asOfDate,
        decimal previousRate,
        decimal currentRate,
        DateTimeOffset firedAt
    )
    {
        if (ruleId == Guid.Empty)
        {
            return Result<Alert>.Failure(
                DomainError.Validation("ALERT_RULE_ID_REQUIRED", "RuleId is required.")
            );
        }

        if (asOfDate == default)
        {
            return Result<Alert>.Failure(
                DomainError.Validation("ALERT_AS_OF_DATE_REQUIRED", "AsOfDate is required.")
            );
        }

        if (previousRate <= 0m)
        {
            return Result<Alert>.Failure(
                DomainError.Validation(
                    "ALERT_PREVIOUS_RATE_NONPOSITIVE",
                    "PreviousRate must be strictly positive."
                )
            );
        }

        if (currentRate < 0m)
        {
            return Result<Alert>.Failure(
                DomainError.Validation(
                    "ALERT_CURRENT_RATE_NEGATIVE",
                    "CurrentRate must not be negative."
                )
            );
        }

        if (firedAt == default)
        {
            return Result<Alert>.Failure(
                DomainError.Validation("ALERT_FIRED_AT_REQUIRED", "FiredAt is required.")
            );
        }

        var observedChangePercent = Math.Abs((currentRate - previousRate) / previousRate) * 100m;

        return Result<Alert>.Success(
            new Alert(
                Guid.NewGuid(),
                ruleId,
                asOfDate,
                previousRate,
                currentRate,
                observedChangePercent,
                firedAt
            )
        );
    }

    /// <inheritdoc/>
    public bool Equals(Alert? other) => other is not null && Id == other.Id;

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();
}
