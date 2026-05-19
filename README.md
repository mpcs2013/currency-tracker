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

## Project documents

- `AGENTS.md` — conventions, "Don't" list, gotchas. **Read this if you are
  an agent session, before doing anything else.**
- `docs/decisions/` — architecture decision records.

## Licence

Apache License 2.0 (see `LICENSE`).
