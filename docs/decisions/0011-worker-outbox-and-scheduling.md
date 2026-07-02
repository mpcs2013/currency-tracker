# 0011 — Worker outbox/inbox on Postgres + Wolverine cron-scheduled ingestion

- **Status:** Accepted
- **Date:** 02.07.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0001-stack-choices.md (stack), 0004-ef-core-persistence.md
  (the ApplicationDbContext the outbox rides), 0006-wolverine-service-location-for-internal-adapters.md
  (the codegen opt-ins the Worker reuses), Phase 5 (handler discovery +
  cascading messages), Phase 7 (Aspire resource + connection-string-from-config),
  Phase 9 (IngestDailyRatesCommand / DailyRatesIngested)

## Context

The Worker is the project's second composition root and has been a bare
IHost since Phase 2.6. Phase 12 makes it the durable, scheduled host for
daily ingestion and (in Part 2) the alert cascade. Cascading messages that
also change state need a transactional outbox so a state change and the
message it triggers can't diverge across a crash.

## Decision

- The Worker hosts the same Application handlers as the Api via
  `builder.UseWolverine(opts => opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly)`.
- Wolverine's transactional inbox/outbox is backed by the EXISTING
  `currencytracker` Postgres, in a dedicated `wolverine` schema
  (`PersistMessagesWithPostgresql(conn, "wolverine")`). One database, one
  transaction across app state + outgoing message.
- EF Core integration (`UseEntityFrameworkCoreTransactions()` +
  `Policies.AutoApplyTransactions()`) joins Wolverine to the DbContext
  transaction; `Policies.UseDurableLocalQueues()` makes the in-process
  cascade durable.
- Daily ingestion is a Wolverine cron-scheduled job
  (`Schedule.CronJob<DailyIngestionScheduleJob>`) at 06:00 UTC. The cron
  expression and the connection string come from IConfiguration.
- New top-level deps: `WolverineFx.EntityFrameworkCore`,
  `WolverineFx.Postgresql`, pinned to the WolverineFx version family
  already in Directory.Packages.props.

## Considered and rejected

- **A message broker (RabbitMQ / Azure Service Bus) for the cascade.**
  Rejected for this phase: the cascade is in-process; a broker is operational
  weight with no current cross-service need. Revisit if a second service
  consumes these messages.
- **A separate scheduler (cron container, Hangfire, Quartz.NET).** Rejected:
  Wolverine already provides durable scheduling once the outbox exists; a
  second scheduler is a second source of truth.
- **A self-rescheduling one-shot scheduled message.** A legitimate pattern
  (`bus.ScheduleAsync` then re-schedule on completion), but it spreads the
  "when does it run" across runtime state instead of one declared cron
  expression, and a missed reschedule silently stops the job.

## Consequences

- The Worker now requires the `currencytracker` connection string at boot
  (fail-fast if absent), exactly like the Api since Phase 8.
- Outbox tables must be provisioned (dev: resource-setup-on-startup;
  Azure: a migration/DDL step in Phase 14).
- Part 2's "no duplicate dispatches on replay" needs BOTH this durability
  AND an idempotency key (12.9). Durability alone is at-least-once.

## Notes

Wolverine scheduling/bootstrap method names are version-sensitive; the names
above reflect the version pinned in Directory.Packages.props at this PR.