# currency-tracker

A learning-by-doing currency tracker built solo with AI agents.
Clean Architecture, .NET 10 LTS, Wolverine, Aspire, Postgres, Redis.

## Current phase

**Phase 0 — Minimal repo bootstrap.** No production code yet. The build plan
runs through Phase 16 (optional React frontend). Deploy is Phase 14; ignore
anything deploy-related until then.

## Running locally

There is nothing to run yet. Phase 7 introduces the Aspire AppHost, after
which `dotnet run --project src/CurrencyTracker.AppHost` will bring up the
full local stack (API, Worker, Postgres, Redis, Keycloak, OTLP collector).

For now, the only things to verify are:

```bash
dotnet --version            # 10.0.300 or newer
csharpier --version         # global tool, used by every PR
gh auth status              # authenticated
```

## Architecture

CurrencyTracker follows the Clean Architecture dependency direction:
`Domain ? Application ? Infrastructure ? (Api | Worker)`. Domain has zero
outbound references; each layer depends only on the layers below it.
Architecture tests under `tests/CurrencyTracker.Architecture.Tests`
fail the build when the contract is violated.

```mermaid
flowchart LR
    %% Production-code layers
    Domain[CurrencyTracker.Domain]
    Application[CurrencyTracker.Application]
    Infrastructure[CurrencyTracker.Infrastructure]
    Api[CurrencyTracker.Api]
    Worker[CurrencyTracker.Worker]

    %% Test projects
    DomainTests[Domain.UnitTests]
    AppTests[Application.UnitTests]
    ArchTests[Architecture.Tests]

    %% Production-code reference arrows (solid; enforced by architecture tests)
    Application --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain
    Api --> Infrastructure
    Api --> Application
    Api --> Domain
    Worker --> Infrastructure
    Worker --> Application
    Worker --> Domain

    %% Test ? production arrows (dashed; convention, not enforced)
    DomainTests -.-> Domain
    AppTests -.-> Application
    AppTests -.-> Domain
    ArchTests -.-> Domain
    ArchTests -.-> Application
    ArchTests -.-> Infrastructure
    ArchTests -.-> Api
    ArchTests -.-> Worker

    %% Styling: src/ blue, tests/ grey
    classDef src fill:#dbeafe,stroke:#1e3a8a,color:#0f172a
    classDef tests fill:#f1f5f9,stroke:#475569,color:#0f172a,stroke-dasharray: 5 3
    class Domain,Application,Infrastructure,Api,Worker src
    class DomainTests,AppTests,ArchTests tests

## Project documents

- `AGENTS.md` — conventions, "Don't" list, gotchas. **Read this if you are
  an agent session, before doing anything else.**
- `docs/decisions/` — architecture decision records.

## Licence

Apache License 2.0 (see `LICENSE`).
