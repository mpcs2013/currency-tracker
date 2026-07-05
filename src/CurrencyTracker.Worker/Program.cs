using CurrencyTracker.Application;
using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Application.Abstractions.Notifications;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.Exceptions; // NotFoundException
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Domain.Exceptions; // DomainException
using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;
using CurrencyTracker.Worker.Configuration;
using CurrencyTracker.Worker.Scheduling;
using JasperFx; // RunJasperFxCommands (from 12.1)
using JasperFx.Resources; // AddResourceSetupOnStartup (see version note)
using Microsoft.EntityFrameworkCore; // DbUpdateException
using Npgsql; // NpgsqlException (transitive via EF provider)
using Quartz;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling; // OnException / RetryWithCooldown / ScheduleRetry
using Wolverine.FluentValidation;
using Wolverine.Postgresql; // PersistMessagesWithPostgresql

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// + 12.11: subscribe the Worker to the app's ingestion telemetry (Phase 9.8)
// and to Wolverine's self-instrumentation. Wolverine's tracing source is
// named "Wolverine"; its meter is per-application ("Wolverine:<app>"), hence
// the wildcard. Without these AddSource/AddMeter calls the spans are emitted
// and silently discarded — pay-for-play.
builder
    .Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(IngestionTelemetry.SourceName).AddMeter("Wolverine*"))
    .WithTracing(tracing =>
        tracing.AddSource(IngestionTelemetry.SourceName).AddSource("Wolverine")
    );

// Registers ApplicationDbContext + the EF repositories, Frankfurter provider,
// cache, clock — the same seam the Api uses (Phase 8). The handlers the Worker
// dispatches resolve their ports through this.
builder.AddInfrastructure();

// The SAME connection string Phase 8 reads and Phase 7's AppHost injects.
// Fail fast at boot if it's missing — the outbox cannot function without it.
var currencyTrackerConnectionString =
    builder.Configuration.GetConnectionString("currencytracker")
    ?? throw new InvalidOperationException(
        "Connection string 'currencytracker' is required for the Wolverine outbox/inbox."
    );

builder
    .Services.AddOptions<WorkerOptions>()
    .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
    .ValidateOnStart();

var ingestSchedule =
    builder.Configuration[$"{WorkerOptions.SectionName}:{nameof(WorkerOptions.IngestSchedule)}"]
    ?? "0 0 6 * * ?";

// + 12.4: Quartz drives the recurring cron. Quartz (3.3.2+) resolves the job
// from DI per fire with a fresh scope, so the job can inject the scoped
// IMessageBus. InTimeZone(Utc) makes "0 0 6 * * ?" mean 06:00 UTC regardless
// of the host clock. Quartz uses its in-memory store here — the *schedule*
// isn't persisted (it's re-declared on every startup), but the *work* is
// durable because the job publishes onto Wolverine's Postgres outbox.
var ingestJobKey = new JobKey("daily-ingestion");
builder.Services.AddQuartz(q =>
{
    q.AddJob<DailyIngestionScheduleJob>(opts => opts.WithIdentity(ingestJobKey));
    q.AddTrigger(opts =>
        opts.ForJob(ingestJobKey)
            .WithIdentity("daily-ingestion-trigger")
            .WithCronSchedule(ingestSchedule, cron => cron.InTimeZone(TimeZoneInfo.Utc))
    );
});
builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

builder.UseWolverine(opts =>
{
    // Local dev: run the durability subsystem in Solo mode so the outbox
    // polling/relay agent starts immediately on this single node instead of
    // waiting on leader election, which has known cold-start hiccups when you
    // stop/start the host in a debugger. Production stays Balanced (the default)
    // so agents distribute across replicas. (DurabilityMode is in the Wolverine
    // namespace — already imported.)
    if (builder.Environment.IsDevelopment())
    {
        opts.Durability.Mode = DurabilityMode.Solo;
    }

    // Same convention discovery as the Api: scan the Application assembly for
    // *Handler types. The Worker is an IHost, not a WebApplication — there is
    // no MapWolverineEndpoints and no WolverineFx.Http here.
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;

    // The ingestion command carries a FluentValidation validator (Phase 9.5);
    // run it here too so a malformed command is rejected before the handler,
    // exactly as on the HTTP side.
    opts.UseFluentValidation();

    // Durable transactional inbox/outbox in the SAME database, "wolverine" schema.
    opts.PersistMessagesWithPostgresql(currencyTrackerConnectionString, "wolverine");

    // Join Wolverine's messaging to the ApplicationDbContext transaction:
    // a handler's SaveChangesAsync and its outgoing/handled messages commit
    // together, or not at all.
    opts.UseEntityFrameworkCoreTransactions();

    // Apply that transactional wrapper to every handler automatically, so no
    // handler needs a [Transactional] attribute.
    opts.Policies.AutoApplyTransactions();

    // Route the in-process cascade through the durable outbox/inbox, so a
    // cascaded message is persisted before it's handled and survives a restart.
    opts.Policies.UseDurableLocalQueues();

    // + 12.12: per-exception-class failure policies. Order matters — first
    // matching policy wins, so the most specific classifications come first.

    // Deterministic pipeline fault (12.8: the alert row is gone). Retrying
    // cannot help; park it for a human, envelope intact.
    opts.OnException<NotFoundException>().MoveToErrorQueue();

    // External-provider fault (ingestion handler throws DomainException when
    // Frankfurter fails). Real backoff, DURABLY scheduled via the outbox —
    // the retry survives a Worker restart mid-wait. Then dead-letter.
    opts.OnException<DomainException>()
        .ScheduleRetry(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15))
        .Then.MoveToErrorQueue();

    // Transient infrastructure fault: cheap immediate-ish retries, then
    // dead-letter.
    opts.OnException<NpgsqlException>()
        .RetryWithCooldown(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(500)
        )
        .Then.MoveToErrorQueue();

    // Transient infrastructure fault, the application's EF writes: EF wraps
    // every SaveChanges-path database failure in DbUpdateException — the raw
    // NpgsqlException never escapes SaveChangesAsync, so the policy above
    // never sees it. Retry is safe AND convergent here: re-execution re-runs
    // the whole handler, so a 12.9 unique-index race resolves itself — the
    // evaluator's skip-query sees the committed winner and returns empty.
    opts.OnException<DbUpdateException>()
        .RetryWithCooldown(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(500)
        )
        .Then.MoveToErrorQueue();

    // + 12.12: one Debug line per handling attempt, so a poison message's
    // retry history reads straight off the logs (and the 12.11 trace).
    opts.Policies.LogMessageStarting(LogLevel.Debug);

    // The ingestion handler depends on internal sealed adapters. Wolverine 6
    // cannot inline-construct internal types, and ServiceLocationPolicy.NotAllowed
    // (the default) forbids the fallback — opt these three ports into service
    // location, exactly as the Api does (ADR 0006).
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IAlertRuleEvaluator>(); // + 12.6
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IAlertRepository>(); // + 12.6
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IAlertNotifier>(); // + 12.8
});

// Dev convenience: create the wolverine_* tables on startup so a fresh clone
// "just works". In Azure (Phase 14) the schema is provisioned by a deploy step,
// not at runtime.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResourceSetupOnStartup();
}

//var host = builder.Build();
//host.Run();

// Forward args to the JasperFx command line so the `describe` / `codegen`
// verbs work in this host, exactly as Phase 5.9 did for the Api. With no
// command argument this behaves identically to host.Run() — the AppHost
// launches the Worker with no args, so production startup is unchanged.
return await builder.Build().RunJasperFxCommands(args);
