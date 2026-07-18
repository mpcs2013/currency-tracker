using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Static telemetry primitives for the alert pipeline stages: the
/// <see cref="ActivitySource"/> emitting the
/// <c>alerts.evaluate_rules</c> and <c>alerts.dispatch</c> spans.
/// The scope name is the shared <c>CurrencyTracker</c> instrumentation
/// scope both hosts already register (9.10 / 12.11), so no additional
/// OpenTelemetry registration is required — or permitted; the name is
/// a contract.
/// </summary>
public static class AlertTelemetry
{
    /// <summary>The instrumentation scope name — the shared app scope.</summary>
    public const string SourceName = "CurrencyTracker";

    /// <summary>Activity source emitting the alert-stage spans.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    /// <summary>
    /// Counter incremented by the number of alerts durably persisted by
    /// an evaluation pass — after the commit, never before (a rolled-back
    /// alert was not triggered).
    /// </summary>
    public static readonly Counter<long> AlertsTriggered = Meter.CreateCounter<long>(
        "alerts.triggered",
        unit: "{alert}",
        description: "Number of alerts persisted and handed to the outbox for dispatch."
    );
}
