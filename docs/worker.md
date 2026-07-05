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

## The alert pipeline

Ingestion cascades through three Application handlers, all durable on the
Worker's Postgres outbox ("wolverine" schema):

    IngestDailyRatesCommand -> IngestDailyRatesHandler
        -> DailyRatesIngested -> EvaluateRulesHandler   (persists Alerts)
            -> AlertTriggered -> DispatchAlertHandler   (IAlertNotifier)

Alert identity is (rule_id, as_of_date), enforced by a unique index
(ADR 0012): the evaluator skips already-alerted rules, and the index
catches concurrent races. Re-ingesting a day is safe end-to-end — the
inbox dedupes replayed ENVELOPES, the business key dedupes replayed
MEANING. The delivery channel is LogAlertNotifier (one structured
Information line per alert, EventId 1200); a real transport replaces the
adapter, not the pipeline.

## Failure handling

Worker-only policies (the Api's HTTP path keeps its ProblemDetails
contract), first match wins:

| Exception          | Policy                                   | Rationale                       |
| ------------------ | ---------------------------------------- | ------------------------------- |
| NotFoundException  | dead-letter immediately                  | deterministic; retry is waste       |
| DomainException    | scheduled retry 5 min, 15 min, then DL   | external provider; slow backoff     |
| NpgsqlException    | cooldown retry 100/250/500 ms, then DL   | transient (Wolverine's ADO ops)     |
| DbUpdateException  | cooldown retry 100/250/500 ms, then DL   | transient (app's EF writes; EF wraps the raw NpgsqlException) |

Dead letters land in wolverine.wolverine_dead_letters with the full
envelope and exception text:

    select id, message_type, exception_type, exception_message
    from wolverine.wolverine_dead_letters;

Fix the cause, then replay (JasperFx exposes dead-letter replay via the
command line / stored procedures — see the Wolverine dead-letter docs for
the pinned version). `LogMessageStarting` (Debug) prints one line per
attempt, so a message's retry history reads straight off the logs.

## Verify

- `dotnet run --project src/CurrencyTracker.AppHost` → dashboard shows `worker`
  Running; `\dt wolverine.*` in the `currencytracker` DB shows the envelope tables.
- Temporarily set `Worker:IngestSchedule` to `*/30 * * * * ?` (every 30s) and watch
  the ingestion run in the dashboard trace, then restore `0 0 6 * * ?`.