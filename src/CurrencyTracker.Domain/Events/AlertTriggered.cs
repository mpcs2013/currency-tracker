namespace CurrencyTracker.Domain.Events;

/// <summary>
/// Domain event raised when an alert rule fires for an observed rate change.
/// </summary>
/// <param name="AlertId">Identifier of the alert that fired.</param>
/// <param name="RuleId">Identifier of the rule that triggered the alert.</param>
/// <param name="ObservedChangePercent">Observed percent change that caused the alert.</param>
/// <param name="FiredAt">Timestamp when the alert fired.</param>
public sealed record AlertTriggered(
    Guid AlertId,
    Guid RuleId,
    decimal ObservedChangePercent,
    DateTimeOffset FiredAt
);
