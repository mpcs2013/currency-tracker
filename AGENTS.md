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

## How to update this file

This file is living memory. When you (the agent or the human) discover:

- a convention worth recording, add it to **Conventions**;
- a footgun the next session shouldn't hit, add it to **Gotchas**;
- a library or pattern the project has rejected, add a one-liner under
  **Don't** *and* link to a `docs/decisions/NNNN-*.md` with the reasoning.

Drift is the failure mode. A file that hasn't been touched in three phases is
probably wrong.
