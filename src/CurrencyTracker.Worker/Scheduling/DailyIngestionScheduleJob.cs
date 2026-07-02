using CurrencyTracker.Application.Abstractions.Time;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Worker.Configuration;
using Microsoft.Extensions.Options;
using Wolverine;

namespace CurrencyTracker.Worker.Scheduling;

/// <summary>
/// Wolverine cron-scheduled job that triggers the daily ingestion. On each
/// run it publishes one <see cref="IngestDailyRatesCommand"/> per configured
/// base currency for the current UTC date. This is an edge trigger only — it
/// holds no business rules; the Phase 9 <c>IngestDailyRatesHandler</c> does
/// the fetch/map/upsert. Registered via
/// <c>opts.Schedule.CronJob&lt;DailyIngestionScheduleJob&gt;(cron)</c> in
/// <c>Program.cs</c>.
/// </summary>
public sealed class DailyIngestionScheduleJob
{
    private readonly IDateTimeProvider _clock;
    private readonly WorkerOptions _options;

    /// <summary>Creates the schedule job.</summary>
    /// <param name="clock">Clock port (Phase 4) used to resolve "today".</param>
    /// <param name="options">Bound <see cref="WorkerOptions"/>.</param>
    public DailyIngestionScheduleJob(IDateTimeProvider clock, IOptions<WorkerOptions> options)
    {
        _clock = clock;
        _options = options.Value;
    }

    /// <summary>
    /// Publishes one ingestion command per configured base for the current
    /// UTC date. Published (not invoked) so the command flows through the
    /// durable local queue rather than blocking the scheduler thread.
    /// </summary>
    /// <param name="bus">The Wolverine message bus, supplied by the scheduler.</param>
    /// <param name="cancellationToken">Token to cancel the run.</param>
    public async Task ExecuteAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var asOf = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        foreach (var baseCurrency in _options.IngestBases)
        {
            await bus.PublishAsync(new IngestDailyRatesCommand(baseCurrency, asOf));
        }
    }
}
