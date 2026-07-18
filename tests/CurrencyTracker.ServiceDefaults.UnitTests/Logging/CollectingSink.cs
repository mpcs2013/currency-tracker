using Serilog.Core;
using Serilog.Events;

namespace CurrencyTracker.ServiceDefaults.UnitTests.Logging;

/// <summary>
/// Test sink that collects emitted <see cref="LogEvent"/>s so tests can
/// assert on properties after the enrichment pipeline has run.
/// </summary>
internal sealed class CollectingSink : ILogEventSink
{
    /// <summary>The events emitted through the pipeline, in order.</summary>
    public List<LogEvent> Events { get; } = [];

    /// <inheritdoc />
    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}
