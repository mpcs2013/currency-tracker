namespace CurrencyTracker.Worker.Configuration;

/// <summary>
/// Strongly-typed Worker configuration bound from the "Worker" section.
/// Both members have safe defaults so a missing section still produces a
/// once-a-day USD ingestion at 06:00 UTC.
/// </summary>
public sealed class WorkerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Worker";

    /// <summary>
    /// Quartz-style cron expression for the daily ingestion job. Six fields:
    /// second minute hour day-of-month month day-of-week. Default
    /// <c>0 0 6 * * ?</c> = 06:00 every day (interpreted on the host clock;
    /// the host runs in UTC).
    /// </summary>
    public string IngestSchedule { get; init; } = "0 0 6 * * ?";

    /// <summary>
    /// Base currencies to ingest on each scheduled run. Default <c>["USD"]</c>.
    /// </summary>
    public string[] IngestBases { get; init; } = ["USD"];
}
