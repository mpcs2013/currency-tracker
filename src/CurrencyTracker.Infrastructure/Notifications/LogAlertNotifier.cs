using CurrencyTracker.Application.Abstractions.Notifications;
using CurrencyTracker.Domain.Alerts;
using Microsoft.Extensions.Logging;

namespace CurrencyTracker.Infrastructure.Notifications;

/// <summary>
/// Structured-log implementation of <see cref="IAlertNotifier"/> — the
/// Phase 12 delivery channel. Writes one Information entry per dispatched
/// <see cref="Alert"/>; a real transport (email/Slack/webhook switched on
/// <c>AlertRule.Channel</c>) replaces this adapter in a later phase
/// without touching the port or the pipeline.
/// </summary>
internal sealed partial class LogAlertNotifier : IAlertNotifier
{
    private readonly ILogger<LogAlertNotifier> _logger;

    public LogAlertNotifier(ILogger<LogAlertNotifier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(Alert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogAlertDispatched(alert.Id, alert.RuleId, alert.ObservedChangePercent, alert.FiredAt);
        return Task.CompletedTask;
    }

    // Notifications own the 1200 EventId band (MigrationRunner 1000s,
    // CurrencySeeder 1100s).
    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Information,
        Message = "ALERT {AlertId}: rule {RuleId} fired — observed change {ObservedChangePercent}% at {FiredAt}."
    )]
    private partial void LogAlertDispatched(
        Guid alertId,
        Guid ruleId,
        decimal observedChangePercent,
        DateTimeOffset firedAt
    );
}
