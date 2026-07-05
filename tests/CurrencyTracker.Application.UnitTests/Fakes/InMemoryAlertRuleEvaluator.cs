using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Domain.Alerts;
using CurrencyTracker.Domain.Currencies;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IAlertRuleEvaluator"/> fake returning a canned list of
/// alerts. Tests arrange <see cref="AlertsToReturn"/>; the handler under
/// test never learns where alerts come from.
/// </summary>
public sealed class InMemoryAlertRuleEvaluator : IAlertRuleEvaluator
{
    /// <summary>Gets or sets the alerts every evaluation returns.</summary>
    public IReadOnlyList<Alert> AlertsToReturn { get; set; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<Alert>> EvaluateAsync(
        CurrencyCode baseCurrency,
        DateOnly asOf,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AlertsToReturn);
    }
}
