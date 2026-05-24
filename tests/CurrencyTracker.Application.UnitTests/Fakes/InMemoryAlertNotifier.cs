using CurrencyTracker.Application.Abstractions.Notifications;
using CurrencyTracker.Domain.Alerts;

namespace CurrencyTracker.Application.UnitTests.Fakes;

/// <summary>
/// In-memory <see cref="IAlertNotifier"/> fake. Appends every dispatched
/// <see cref="Alert"/> to <see cref="SentAlerts"/> so tests can assert on
/// what was sent and in which order. Cancellation is honoured before the
/// alert is recorded.
/// </summary>
public sealed class InMemoryAlertNotifier : IAlertNotifier
{
    private readonly List<Alert> _sentAlerts = [];

    /// <summary>
    /// Gets the ordered list of alerts dispatched via
    /// <see cref="SendAsync"/> since this instance was created.
    /// </summary>
    public IReadOnlyList<Alert> SentAlerts => _sentAlerts;

    /// <inheritdoc />
    public Task SendAsync(Alert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sentAlerts.Add(alert);
        return Task.CompletedTask;
    }
}
