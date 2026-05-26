# AGENTS.md 

This file is the canonical project memory for AI agent sessions. Read it before
touching any other file. Update it whenever you discover a convention this
project has that isn't obvious from the code, or whenever you hit a footgun
that the next session shouldn't.

## What this project is

A learning-by-doing currency tracker built solo with AI agents.
Clean Architecture, .NET 10 LTS (C# 14), Wolverine for messaging,
Aspire for local orchestration, Postgres for persistence, Redis for caching.

Backend-first. A React frontend is optional and lives in Phase 16. Don't
suggest UI work before then.

## Current phase

**Phase 0 — Minimal repo bootstrap.** No `.csproj` files exist yet. The
first project lands in Phase 2 (solution skeleton).

## Architecture

Clean Architecture, strict dependency direction:

    Domain  ←  Application  ←  Infrastructure  ←  (Api | Worker)

- `Domain` has zero NuGet dependencies — pure C#.
- `Application` defines ports (interfaces) and message contracts.
- `Infrastructure` implements ports (EF Core, Redis, HTTP clients).
- `Api` and `Worker` are composition roots (Aspire-orchestrated locally).

## Project layout

`src/` holds production code. `tests/` holds test projects (added in
Phase 2.2). The names below are contracts: an agent that wants to call
the Domain project `CurrencyTracker.Core`, or to add a `Shared` or
`Common` project, should **stop and open an ADR** instead. Five layers
is the contract for this build plan.

```
src/CurrencyTracker.Domain          ← pure C#, zero ProjectReferences,
                                       (almost) zero PackageReferences

src/CurrencyTracker.Application     ← references Domain only;
                                       defines ports and CQRS messages

src/CurrencyTracker.Infrastructure  ← references Application + Domain;
                                       EF Core, Redis, HttpClient adapters

src/CurrencyTracker.Api             ← references Infrastructure + Application
                                       + Domain; HTTP composition root

src/CurrencyTracker.Worker          ← references Infrastructure + Application
                                       + Domain; background-job composition root
                                       (does NOT reference Api)

src/CurrencyTracker.AppHost         ← Phase 7 addition (Aspire orchestrator);
                                       references Api and Worker for the
                                       Aspire SDK to generate Projects.*
                                       handles; never references Application,
                                       Infrastructure, or Domain; never runs
                                       in Azure (Phase 14 deploys Api and
                                       Worker directly)

src/CurrencyTracker.ServiceDefaults ← Phase 7 addition (Aspire shared
                                       library); referenced by Api and
                                       Worker; owns OTel + health +
                                       resilience defaults; deployed with
                                       each host that references it
```


| Project          | References                 | Outbound NuGet (Phase 2.1)       | Composition root? |
| ---------------- | -------------------------- | -------------------------------- | ----------------- |
| `Domain`         | (none)                     | (none)                           | No                |
| `Application`    | `Domain`                   | (none)                           | No                |
| `Infrastructure` | `Application`, `Domain`    | (none — Phase 8+ adds EF Core)   | No                |
| `Api`            | `Infrastructure`, `App.`, `Dom.` | `Microsoft.AspNetCore.OpenApi` | Yes (HTTP)        |
| `Worker`         | `Infrastructure`, `App.`, `Dom.` | (none — Phase 12 adds Wolverine) | Yes (jobs)        |
| `AppHost`        | `Api`, `Worker` (run-time only)  | `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Redis` (SDK adds `Aspire.Hosting.AppHost`) | No |
| `ServiceDefaults`| (none of the layer projects)     | `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery`, `OpenTelemetry.*` family | No |

**Reading the table:** `Api` and `Worker` are the only rows with **Yes**
in the last column. That's the rule: composition roots compose, layers
expose. If a future PR wants to add a third `Yes` (e.g. a CLI tool, a
gRPC host, a Functions app), that's an ADR-worthy decision — the
current pipeline assumes exactly two.

## Ports and adapters

A **port** is an interface defined by the Application layer that describes a
capability Application needs from the outside world but won't implement itself.
The classic phrasing: the port is the socket on the wall, the adapter is the
plug on the device. **Application owns the socket shape; Infrastructure owns
the plug.**

Ports live in `src/CurrencyTracker.Application/Abstractions/<Capability>/`.
Adapters live in `src/CurrencyTracker.Infrastructure/<Capability>/`.

### Ports defined in Phase 4

| Port                       | Purpose                                                | Adapter location (when shipped)                          | Phase |
| -------------------------- | ------------------------------------------------------ | -------------------------------------------------------- | ----- |
| `IDateTimeProvider`        | Replace `DateTimeOffset.UtcNow` so tests can fix time. | `Infrastructure/Time/SystemDateTimeProvider.cs`          | 4     |
| `ICacheService`            | Get/set/remove cache entries; cache-aside helper.      | `Infrastructure/Caching/RedisCacheService.cs`            | 10    |
| `IExchangeRateProvider`    | Fetch an FX-rate snapshot from an external source.     | `Infrastructure/Providers/FrankfurterRateProvider.cs`    | 9     |
| `IAlertNotifier`           | Dispatch an `Alert` to a configured channel.           | `Infrastructure/Notifications/LogAlertNotifier.cs`       | 12    |
| `ICurrentUser`             | Identity / claims for the current request.             | `Infrastructure/Security/HttpContextCurrentUser.cs`      | 11    |
| `IUnitOfWork`              | Atomically persist outstanding domain changes.         | `Infrastructure/Persistence/EfUnitOfWork.cs`             | 8     |
| `ICurrencyRepository`      | Load and save `Currency` aggregates.                   | `Infrastructure/Persistence/EfCurrencyRepository.cs`     | 8     |
| `IExchangeRateRepository`  | Load and save `RateSnapshot` aggregates.               | `Infrastructure/Persistence/EfExchangeRateRepository.cs` | 8     |

### The rule that drove the Phase 4 repository move

- **Interfaces live with the layer that *needs* them, not with the layer that
  *defines the model*.** The repository interfaces transiently sat in Domain
  during Phase 3 so Phase 4 could move them deliberately. The mover is
  Application, which is what calls them; Domain doesn't call repositories.

### Fakes live with tests

- Every port has an in-memory **fake** under
  `tests/CurrencyTracker.Application.UnitTests/Fakes/`, one class per port,
  `InMemory<PortName>` (e.g. `InMemoryExchangeRateProvider`).
- Fakes are real working implementations with their own state, **not** mocks.
  Tests interact with the fake by seeding state (`provider.Seed(usd, today,
  snapshot)`) and asserting on state afterward.
- Mocks (NSubstitute) are reserved for cases where the **call shape** is the
  contract — e.g. "did the exception handler invoke the logger". For
  state-shaped behaviour, prefer a fake.

## Testing conventions

`tests/` holds three classes of test project. **Unit tests** under
`tests/CurrencyTracker.Domain.UnitTests` and
`tests/CurrencyTracker.Application.UnitTests` exercise pure code paths
in-memory and run in milliseconds per test. **Integration tests** under
`tests/CurrencyTracker.Infrastructure.IntegrationTests` (lands Phase 8)
and `tests/CurrencyTracker.Api.IntegrationTests` (lands Phase 9) spin
up real PostgreSQL and Redis containers via Testcontainers and run on
the order of seconds per test. **Architecture tests** under
`tests/CurrencyTracker.Architecture.Tests` inspect compiled IL via
`NetArchTest.Rules` and enforce the dependency-direction contract
named in **Project layout** above.

| Project                                         | Style                              | Discovers tests via       | Lands in phase |
| ----------------------------------------------- | ---------------------------------- | ------------------------- | -------------- |
| CurrencyTracker.Domain.UnitTests                | xUnit v3 + FluentAssertions        | MTP v1 (template default) | 2.7            |
| CurrencyTracker.Application.UnitTests           | xUnit v3 + FluentAssertions + NSub | MTP v1 (template default) | 2.7            |
| CurrencyTracker.Architecture.Tests              | xUnit v3 + NetArchTest.Rules       | MTP v1 (template default) | 2.8            |
| CurrencyTracker.Infrastructure.IntegrationTests | xUnit v3 + Testcontainers          | MTP v1                    | 8.x            |
| CurrencyTracker.Api.IntegrationTests            | xUnit v3 + Alba + Testcontainers   | MTP v1                    | 9.x            |

### Test-writing rules

- File-scoped namespaces in test files (same convention as `src/`).
- `[Fact] public async Task`, never `async void`.
- One concept per test method; AAA layout (Arrange / Act / Assert) with
  blank lines separating the three blocks.
- `[InlineData]` over `[MemberData]` when the data fits on one line.
- `NSubstitute` for substitutes. No Moq, no FakeItEasy.
- `FluentAssertions` `Should()` chains; one assertion per chain unless
  a single `AssertionScope` deliberately batches them.
- Each `src/` project has a single internal-only `*AssemblyAnchor` type
  to support `NetArchTest.Rules`' `typeof(Anchor).Assembly` lookup. The
  anchor types are empty `public sealed class`es with no members.

### Test → production direction

Test projects under `tests/` reference projects under `src/` (the
inverse of the production-code direction). This is not a violation of
the architecture contract — the contract is about `src/`-to-`src/`
references. The architecture test in `Architecture.Tests` inspects
only assemblies compiled from `src/`.

```
tests/Domain.UnitTests        ───▶ src/Domain
tests/Application.UnitTests   ───▶ src/Application  (+ Domain transitively)
tests/Architecture.Tests      ───▶ all src/ assemblies (read-only inspection)
tests/*.IntegrationTests      ───▶ src/Infrastructure or src/Api  (Phases 8–9)
```

## Agent principles

Every agent session — whether Claude Code, Cursor, Copilot, Codex, or a
plain Claude chat — inherits these rules. They are the floor. The mindset
taxonomy below narrows them; `Conventions` and `Don't` further constrain
them. When in doubt, this section wins.

**Agents MUST:**

- Explain the goal in plain English **before** generating code, especially
  when scaffolding something new. Plan first; code second.
- Generate failing tests first when the issue has testable behaviour, then
  implementation. Red, then green.
- Respect architectural boundaries: `Domain` has no outside refs;
  `Application` defines ports and depends on Domain only; `Infrastructure`
  implements ports; `Api` and `Worker` compose them. Don't add a reference
  that the dependency-direction diagram in *Architecture* (above) forbids.
- Call out security concerns proactively: secrets, authorisation, input
  validation, unsafe defaults, anything that would fail a basic OWASP top-10
  walk-through.
- Call out observability gaps: meaningful operations should have spans;
  meaningful state changes should have structured logs with `traceId`;
  errors should be logged once, at the boundary that handled them.
- Stop and ask when uncertain about package versions, API shapes, or Azure
  resource names. Don't guess — web-search and verify.

**Agents MUST NOT:**

- Merge code. You merge; the agent suggests and reviews.
- Introduce a new top-level NuGet, npm, or Terraform-module dependency
  without a corresponding entry in `docs/decisions/`. Lock files exist for
  a reason.
- Bypass architectural rules when pushed. "Just put this in `Domain` for
  now" is not a valid escape hatch. Bring the rule back up explicitly if it
  no longer fits, and write an ADR.
- Drift from `AGENTS.md`. When conventions in this file conflict with the
  agent's training-data instinct, this file wins. If the file is wrong,
  update it in the same PR.
- Add `IRequest<T>` / `IRequestHandler<T>` marker interfaces. Wolverine
  discovers handlers by convention; markers fight the framework. (See
  Decision 0001 and Phase 5 of the build plan.)
- Add `<PackageVersion>` entries to `Directory.Packages.props` for packages
  no `.csproj` yet references. Speculative pins rot.

## The eleven-mindset taxonomy

You don't create eleven Claude Projects, eleven Cursor rule files, or
eleven Copilot custom-instruction sets. You use this table to decide
*which mindset to invoke* when starting an issue. Most issues map cleanly
to one row. Some span two — that's fine, name both at the top of your
prompt.

| Mindset                | Owns                                                                                              | Refuses                                          |
| ---------------------- | ------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| **Domain**             | DDD modelling, value objects, aggregates, invariants, domain events                               | Anything outside `src/CurrencyTracker.Domain`    |
| **Application**        | Ports, CQRS messages, Wolverine handlers, validators                                              | Direct EF Core / Redis / HTTP code               |
| **Infrastructure**     | EF Core, Redis, HTTP adapters, Keycloak / Entra client wiring                                     | Domain logic, business rules                     |
| **API**                | `WolverineFx.Http` endpoints, DTOs, ProblemDetails, OpenAPI                                       | Domain rules, infrastructure                     |
| **Worker**             | Wolverine scheduling, outbox / inbox, idempotency, retries, dead-letter handling                  | UI, API surface                                  |
| **Testing**            | xUnit v3, FluentAssertions, NSubstitute, Testcontainers, Alba, test-pyramid balance               | Writing production code (suggests, never writes) |
| **Security**           | OWASP review, JWT validation, secret handling, RBAC, Key Vault, threat modelling                  | Functional features (review-only)                |
| **Observability**      | OpenTelemetry spans, metrics, Serilog enrichers, dashboards, alerts                               | Functional logic                                 |
| **Azure / Deployment** | Terraform, Container Apps, ACR, Key Vault, OIDC federation, App Insights wiring, CI/CD workflows  | Application code                                 |
| **Frontend**           | React + TypeScript scaffolding, API client, Keycloak / Entra SPA flow, Vite config (Phase 16)     | Backend code, infrastructure                     |
| **Documentation**      | `README.md`, `docs/`, decision records, architecture diagrams, onboarding text                    | Code (read-only)                                 |

**Invocation pattern.** Open every session with one line:

    Mindset: Application + Security. Issue: <paste the GitHub issue here>.

That is the entire ceremony. The full per-issue workflow lives in
`docs/workflow.md`; the reusable prompts live in `docs/prompts.md`.

## Conventions

- One PR = one concept. Resist combining. Aim for 1–2 hours of work per PR.
- Tests written first where applicable (red → green → refactor).
- Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`,
  `test:`). The Phase prefix in the scope: `feat(5.3): ...`.
- Branch naming: `<type>/<phase>-<short-desc>`. Example:
  `feat/5.3-currency-pair-handler`.
- Squash-merge only. Linear history on `main`.
- File-scoped namespaces (`namespace Foo;`, not `namespace Foo { }`).
- `readonly record struct` for value objects.
- `sealed record` / `sealed class` with private setters + static `Create`
  factories for domain types.
- Async methods take `CancellationToken` with no default value.
- XML doc comments on every public member.

## Don't

- **Don't upgrade package versions** without an explicit request. Lock files
  exist for a reason; `Directory.Packages.props` is the single source of truth.
- **Don't introduce a new top-level dependency** without checking
  `docs/decisions/` first — the rejection might already be on record.
- **Don't reach for MediatR.** Wolverine is the message bus. (Decision 0001.)
- **Don't add CodeQL, multi-OS CI matrices, or separate deploy workflows in
  Phase 0.** Those are Phase 13 and Phase 14 respectively. The build plan
  documents the reasons.
- **Don't add abstractions for hypothetical future callers.** Add an
  abstraction when the second caller appears.
- **Don't add `<PackageVersion>` entries** to `Directory.Packages.props` for
  packages no `.csproj` yet references.
- **Don't add `IRequest<T>` / `IRequestHandler<T>`-style marker interfaces.**
  Wolverine discovers handlers by convention; markers fight the framework.
  (Phase 5.)
- **Don't auto-apply EF Core migrations on app start in production.** Local
  dev / Aspire only. (Phase 8.)
- Don't modify AGENTS.md, workflow.md, prompts.md, domain.md, ports.md,
  0001-stack-choices.md, or any file under docs/decisions/ unless the issue
  explicitly names that file as the deliverable. Surface the proposed change
  in the PR description instead.

## Quality gates

Every PR runs locally and in CI:

- `csharpier format .` (and `csharpier --check .` in CI).
- `dotnet format --verify-no-changes` (style + analyzer rules).
- `dotnet build --configuration Release` (treats warnings as errors).
- `dotnet test --configuration Release --no-build`.
- Until Phase 2, build and test are skipped by the CI guard (no `.cs` files).

## Gotchas (update this list as agents hit footguns)

- `actions/setup-dotnet@v6` does not exist. Current is **v5**.
  `actions/checkout` is on v6 — they have independent release cadences.
- `setup-dotnet` reads `global.json` if `dotnet-version` is left empty. Don't
  pin a version in the workflow; let `global.json` be the single source.
- `dotnet format` errors against an empty repo. Hence the "Detect C# sources"
  guard step in `ci.yml`.
- `Microsoft.AspNetCore.OpenApi` is built into ASP.NET Core 10. The
  `WithOpenApi(...)` extension method was deprecated in .NET 10 RC1.
  Don't suggest Swashbuckle unless an interactive Swagger UI is explicitly
  needed — and even then, prefer `Scalar.AspNetCore` or `NSwag` over
  Swashbuckle.
- The .NET 10 `webapi` and `worker` templates ship with sample code
  (`WeatherForecast` record + `MapGet("/weatherforecast", …)` for
  `webapi`; a `Worker : BackgroundService` looping `LogInformation`
  every second for `worker`). Strip them in the **same PR** that runs
  `dotnet new` — don't leave them in "for now". The TreatWarningsAsErrors
  flag in `Directory.Build.props` will not catch the cruft (it compiles
  cleanly); only a reviewer's eyes catch it.
- xUnit v3 is the version this codebase uses (3.2.2+). Copilot will frequently
  suggest v2 idioms — `async void`, the `xunit` package instead of `xunit.v3`,
  `[Theory(DisplayName = ...)]`. Reject every v2 borrowing; v3's analyzer
  surfaces the rejection at build time but the diff review is faster.
- All `ILogger` calls in production code must use `[LoggerMessage]`
  source-generated methods, not the `LoggerExtensions.LogXxx(...)`
  convenience overloads. CA1848 is treated as an error. Declare the
  containing class `partial` and add a `static partial void` method
  per log call. See the four handlers in
  `src/CurrencyTracker.Api/ErrorHandling/` for the pattern.
- `[LoggerMessage]` call sites must not pass `exception.GetType().FullName`
  (or any non-trivial expression) as a log parameter — CA1873 flags it
  as potentially-wasted work when the level is disabled. Pass the
  `Exception` object itself; the source generator threads it into the
  structured `@Exception` field which sinks render with the full type
  name and stack trace, *only* when the level is enabled.
- Don't add Postgres or Redis connection strings to
  `src/CurrencyTracker.Api/appsettings.json` or
  `src/CurrencyTracker.Worker/appsettings.json`. Aspire injects them as
  `ConnectionStrings__currencytracker` and `ConnectionStrings__cache`
  environment variables when the AppHost launches each project;
  `IConfiguration.GetConnectionString("currencytracker")` resolves to
  the injected value automatically. A connection string in
  `appsettings.json` would shadow the Aspire-injected one and break the
  local-vs-Azure parity story Phase 14's Key Vault flow depends on.

## How to update this file

This file is living memory. When you (the agent or the human) discover:

- a convention worth recording, add it to **Conventions**;
- a footgun the next session shouldn't hit, add it to **Gotchas**;
- a library or pattern the project has rejected, add a one-liner under
  **Don't** *and* link to a `docs/decisions/NNNN-*.md` with the reasoning.

Drift is the failure mode. A file that hasn't been touched in three phases is
probably wrong.

- a mindset whose Owns / Refuses prose has grown past ~300 lines and needs
  its own `docs/agents/<name>.md` file — split it, link it from the table,
  and write a one-line ADR explaining the split.
