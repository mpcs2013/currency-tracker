using CurrencyTracker.Application.Abstractions.Time;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Worker.Configuration;
using Microsoft.Extensions.Options;
using Quartz;
using Wolverine;

namespace CurrencyTracker.Worker.Scheduling;

/// <summary>
/// Quartz.NET job that triggers the daily ingestion. On each run it publishes
/// one <see cref="IngestDailyRatesCommand"/> per configured base currency for
/// the current UTC date. This is an edge trigger only — it holds no business
/// rules; the Phase 9 <c>IngestDailyRatesHandler</c> does the fetch/map/upsert.
/// Registered with a cron trigger via <c>AddQuartz</c> in <c>Program.cs</c>.
/// Quartz resolves the job from the container per fire (scoped since Quartz
/// 3.3.2), so <see cref="IMessageBus"/> is constructor-injected here.
/// </summary>
public sealed class DailyIngestionScheduleJob : IJob
{
    private readonly IMessageBus _bus;
    private readonly IDateTimeProvider _clock;
    private readonly WorkerOptions _options;

    /// <summary>Creates the schedule job.</summary>
    /// <param name="bus">Wolverine message bus; the command is published onto the durable queue.</param>
    /// <param name="clock">Clock port (Phase 4) used to resolve "today".</param>
    /// <param name="options">Bound <see cref="WorkerOptions"/>.</param>
    public DailyIngestionScheduleJob(
        IMessageBus bus,
        IDateTimeProvider clock,
        IOptions<WorkerOptions> options
    )
    {
        _bus = bus;
        _clock = clock;
        _options = options.Value;
    }

    /// <summary>
    /// Publishes one ingestion command per configured base for the current UTC
    /// date. Published (not invoked) so the command flows through the durable
    /// local queue rather than blocking the Quartz worker thread. Quartz supplies
    /// the <see cref="IJobExecutionContext"/>, whose <c>CancellationToken</c>
    /// signals scheduler shutdown.
    /// </summary>
    /// <param name="context">Quartz execution context for this fire.</param>
    public async Task Execute(IJobExecutionContext context)
    {
        var asOf = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        foreach (var baseCurrency in _options.IngestBases)
        {
            await _bus.PublishAsync(new IngestDailyRatesCommand(baseCurrency, asOf));
        }
    }
}
