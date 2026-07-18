using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CurrencyTracker.Application.Messaging;

/// <summary>
/// Static telemetry primitives for the ingestion slice: the
/// <see cref="ActivitySource"/> that emits the <c>ingest.daily_rates</c>
/// span and the <see cref="Meter"/> that owns the <c>rates.ingested</c>
/// counter. The source/meter name is the application name so the
/// OpenTelemetry tracing/metrics wiring in <c>ServiceDefaults</c> exports
/// them without extra registration.
/// </summary>
public static class IngestionTelemetry
{
    /// <summary>The instrumentation scope name (matches the app name).</summary>
    public const string SourceName = "CurrencyTracker";

    /// <summary>Activity source emitting the ingestion span.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    /// <summary>
    /// Counter incremented by the number of rates persisted on a
    /// successful ingestion.
    /// </summary>
    public static readonly Counter<long> RatesIngested = Meter.CreateCounter<long>(
        "rates.ingested",
        unit: "{rate}",
        description: "Number of exchange rates persisted by daily ingestion."
    );

    /// <summary>
    /// Counter incremented once per failed ingestion attempt, tagged
    /// with the bounded <c>error.code</c> failure vocabulary. Composes
    /// with the 12.12 retry policies: retried attempts each count, so
    /// failures minus dead-letters reads as retry-recovered attempts.
    /// </summary>
    public static readonly Counter<long> IngestionFailures = Meter.CreateCounter<long>(
        "ingestion.failures",
        unit: "{failure}",
        description: "Number of failed daily-ingestion attempts, by error code."
    );
}
