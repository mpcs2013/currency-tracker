# 0004 — EF Core + Npgsql persistence

- **Status:** Accepted
- **Date:** 2026-05-27
- **Authors:** @your-handle
- **Supersedes:** —
- **Related:** [`0001-stack-choices.md`](./0001-stack-choices.md),
  [`0002-clean-architecture-dependency-direction.md`](./0002-clean-architecture-dependency-direction.md)

## Context

Phase 4 defined three persistence ports in `src/CurrencyTracker.Application/Abstractions/Persistence/`:
`ICurrencyRepository`, `IExchangeRateRepository`, and `IUnitOfWork`. Phase 7 stood up a Postgres
container behind the Aspire AppHost with the connection string injected into Api as
`ConnectionStrings__currencytracker`. Phase 8 lands the adapters.

The question is *which EF Core integration pattern* and *which Npgsql provider package* — not whether
to use EF Core (that was settled in 0001).

## Decision

Phase 8 uses:

- `Microsoft.EntityFrameworkCore` (the runtime) + `Microsoft.EntityFrameworkCore.Design` (build-time)
  + `Npgsql.EntityFrameworkCore.PostgreSQL` (the provider). Versions pinned in
  `Directory.Packages.props`.
- A plain `builder.Services.AddDbContext<ApplicationDbContext>(...)` registration in Api's
  `Program.cs`, reading the connection string from `IConfiguration.GetConnectionString("currencytracker")`.
- `RateSnapshot.Rates` configured as `OwnsMany` (an owned-type collection) — not a separate
  `DbSet<ExchangeRate>` with a foreign key. This honours the Phase 3.5 aggregate-root decision:
  `ExchangeRate` is queryable only via its owning snapshot.
- `CurrencyCode` ⇄ `string` value-conversion via `HasConversion<string>` with paired delegates
  (`code => code.Value` writing, `raw => CurrencyCode.Create(raw).Value` reading).
- A `MigrationRunner : IHostedService` registered only when `env.IsDevelopment()`. Production
  migrations are a Phase 14 deploy-pipeline concern, not a runtime concern.

## Considered and rejected

- **`Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`** as the registration shape. The Aspire
  integration adds DbContext pooling, retries, an Aspire-shaped health check, and a configuration
  overlay keyed on `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:<ContextName>`. Useful if/when
  the surface earns its way in — but it adds a third top-level package, a different bootstrap shape,
  and a configuration precedence rule, none of which Phase 8's scope wants. Plain
  `AddDbContext<>` + `UseNpgsql` is the right floor; the Aspire integration is a future upgrade
  if a real need appears.

- **A typed `ValueConverter<CurrencyCode, string>` class** instead of inline `HasConversion<string>`
  delegates. Equivalent runtime behaviour; more code; harder to follow at the configuration
  call-site. Inline conversion wins on legibility.

- **A `string` shadow property on `Currency`/`ExchangeRate` etc, with a domain-level rule that
  "treat the string as a CurrencyCode"**. This is the EF6 idiom and erodes the Phase 3.1 decision
  to make `CurrencyCode` a value object — every read path would have to remember to parse.
  Rejected.

- **Running migrations at Api startup in Production.** The convenience is real (no separate deploy
  step). The cost — schema drift between Container App revisions during a rolling deployment, the
  Api refusing to start if the migration fails, lock contention if two replicas race — is worse.
  Phase 14's deploy pipeline applies migrations once, before the new revision goes live.

## Verified

- **2026-06-01 — Observability**: confirmed Npgsql spans appear in the Aspire
  dashboard for migration, seed, and `GET /ping` traces. Parameters
  redacted as `$N` placeholders; no sensitive-data-logging concerns.
  Verified by issue 8.11.
