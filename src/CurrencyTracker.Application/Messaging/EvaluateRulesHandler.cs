using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Domain.Events;
using Wolverine;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Wolverine handler for the cascading <see cref="DailyRatesIngested"/>
/// event — the pipeline's evaluation stage. Asks
/// <see cref="IAlertRuleEvaluator"/> which enabled rules fired for the
/// ingested base/date, persists each fired <see cref="Domain.Alerts.Alert"/>,
/// and cascades one <see cref="AlertTriggered"/> message per alert by
/// returning <see cref="OutgoingMessages"/>. Under the Part 1
/// transactional middleware, the alert rows and the cascaded envelopes
/// commit in one transaction — the outbox guarantee this phase exists
/// to demonstrate.
/// </summary>
public static class EvaluateRulesHandler
{
    /// <summary>
    /// Handles the post-ingestion evaluation.
    /// </summary>
    /// <param name="event">The cascaded ingestion event (base + date + count).</param>
    /// <param name="evaluator">Rule-evaluation port (12.5).</param>
    /// <param name="alerts">Alert persistence port.</param>
    /// <param name="unitOfWork">Unit-of-work port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Zero or more cascading <see cref="AlertTriggered"/> messages.</returns>
    public static async Task<OutgoingMessages> Handle(
        DailyRatesIngested @event,
        IAlertRuleEvaluator evaluator,
        IAlertRepository alerts,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken
    )
    {
        var fired = await evaluator.EvaluateAsync(@event.Base, @event.AsOf, cancellationToken);

        var messages = new OutgoingMessages();
        foreach (var alert in fired)
        {
            await alerts.AddAsync(alert, cancellationToken);
            messages.Add(
                new AlertTriggered(
                    alert.Id,
                    alert.RuleId,
                    alert.ObservedChangePercent,
                    alert.FiredAt
                )
            );
        }

        // No alerts, no writes — skip the commit round trip. (The Wolverine
        // transaction still wraps the handler; this avoids an empty
        // SaveChangesAsync, mirroring Phase 10's "don't touch what didn't
        // change" discipline.)
        if (fired.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return messages;
    }
}
