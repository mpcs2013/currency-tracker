using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Application.Abstractions.Notifications;

/// <summary>
/// Dispatches a fired <see cref="Alert"/> to whatever delivery channel
/// the owning <see cref="AlertRule"/> specifies. The adapter switches on
/// <c>alert.Rule.Channel</c> internally; callers always use this single
/// method regardless of how many channels are supported.
/// </summary>
/// <remarks>
/// <c>SendAsync</c> returns <see cref="Task"/>, not
/// <see cref="Task{TResult}"/>. Delivery confirmation is asynchronous for
/// most channels; exposing a <c>bool</c> result here would misrepresent
/// semantics. Bounce and delivery-receipt tracking is a Phase 12 outbox
/// concern.
/// </remarks>
public interface IAlertNotifier
{
    /// <summary>
    /// Dispatches <paramref name="alert"/> to the configured delivery channel.
    /// </summary>
    /// <param name="alert">The alert to dispatch. Must not be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the underlying I/O.</param>
    /// <returns>A <see cref="Task"/> that completes when dispatch is handed off.</returns>
    Task SendAsync(Alert alert, CancellationToken cancellationToken);
}
