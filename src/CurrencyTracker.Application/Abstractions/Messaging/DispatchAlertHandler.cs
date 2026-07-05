using CurrencyTracker.Application.Abstractions.Notifications;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Exceptions;
using CurrencyTracker.Domain.Events;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Wolverine handler for <see cref="AlertTriggered"/> — the pipeline's
/// terminal stage. Reloads the persisted alert (the message is a durable
/// pointer; the row is the truth) and hands it to
/// <see cref="IAlertNotifier"/>. Cascades nothing: the pipeline ends here.
/// </summary>
public static class DispatchAlertHandler
{
    /// <summary>
    /// Handles the dispatch of a fired alert.
    /// </summary>
    /// <param name="event">The cascaded alert-fired event.</param>
    /// <param name="alerts">Alert persistence port, for the reload.</param>
    /// <param name="notifier">Delivery-channel port (12.7's log adapter for now).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when dispatch is handed off.</returns>
    /// <exception cref="NotFoundException">Thrown when no alert with the
    /// event's <c>AlertId</c> exists — a structural fault (the outbox
    /// commits the row and the envelope together), routed to the
    /// dead-letter table by 12.12's policy.</exception>
    public static async Task Handle(
        AlertTriggered @event,
        IAlertRepository alerts,
        IAlertNotifier notifier,
        CancellationToken cancellationToken
    )
    {
        var alert =
            await alerts.GetByIdAsync(@event.AlertId, cancellationToken)
            ?? throw new NotFoundException("Alert", @event.AlertId.ToString());

        await notifier.SendAsync(alert, cancellationToken);
    }
}
