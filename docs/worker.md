# The Worker host

`CurrencyTracker.Worker` is the project's second composition root. It runs the
same Wolverine handlers as the Api (convention discovery over the Application
assembly) but is an `IHost`, not a `WebApplication` — no HTTP, no JWT, no
ProblemDetails.

## Durability

Wolverine's transactional inbox/outbox is backed by the `currencytracker`
Postgres (`wolverine` schema). `UseEntityFrameworkCoreTransactions()` +
`AutoApplyTransactions()` make a handler's `SaveChangesAsync` and its
outgoing/handled messages commit in one transaction; `UseDurableLocalQueues()`
routes the in-process cascade through that store. The connection string comes
from `IConfiguration` (`currencytracker`), never a literal.

## Scheduled ingestion

`DailyIngestionScheduleJob` (a Quartz `IJob`) runs daily at 06:00 UTC (cron
`Worker:IngestSchedule`, default `0 0 6 * * ?`). Quartz owns the cadence; the job
publishes one `IngestDailyRatesCommand` per `Worker:IngestBases` entry for the
current UTC date onto the Wolverine outbox. The trigger is registered with
`InTimeZone(TimeZoneInfo.Utc)`, so the cron means 06:00 UTC regardless of the host
clock. Quartz uses its in-memory store — the schedule is re-declared each startup;
work durability lives in the Wolverine outbox.

## What is NOT wired yet (Part 1)

The ingestion handler returns the cascading `DailyRatesIngested` event. In
Part 1 nobody consumes it, so Wolverine logs "no handler" and drops it — this is
expected. Part 2 adds `EvaluateRulesHandler` (consumes `DailyRatesIngested`),
`DispatchAlertHandler`, the `(ruleId, date)` idempotency, the pipeline test, the
end-to-end trace, and the retry/dead-letter policy.

## Verify

- `dotnet run --project src/CurrencyTracker.AppHost` → dashboard shows `worker`
  Running; `\dt wolverine.*` in the `currencytracker` DB shows the envelope tables.
- Temporarily set `Worker:IngestSchedule` to `*/30 * * * * ?` (every 30s) and watch
  the ingestion run in the dashboard trace, then restore `0 0 6 * * ?`.