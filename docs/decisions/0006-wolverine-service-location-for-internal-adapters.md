# 0006 — Wolverine service location for internal adapters

- **Status:** Accepted
- **Date:** 2026-05-28
- **Authors:** @your-handle
- **Supersedes:** —
- **Related:** [`0003-wolverine-no-marker-interfaces.md`](./0003-wolverine-no-marker-interfaces.md),
  [`0004-ef-core-persistence.md`](./0004-ef-core-persistence.md)

## Context

Phase 9's `IngestDailyRatesHandler` is the first Wolverine handler whose
dependencies resolve to `internal sealed` concrete types: the Frankfurter
provider (`IExchangeRateProvider`) and the EF Core repository and
unit-of-work (`IExchangeRateRepository`, `IUnitOfWork`). The cross-layer
guardrails require Infrastructure adapters to be `internal sealed` —
implementation details behind public ports, not part of any public
surface.

Wolverine 6 generates message-handler dispatch code into a separate
assembly and prefers to inline-construct each dependency, which requires
the concrete type to be **public**. For an internal concrete it would
otherwise emit a runtime service-locator call — but Wolverine 6's default
`ServiceLocationPolicy.NotAllowed` forbids that and throws
`InvalidServiceLocationException` at host startup.

So the encapsulation rule (adapters internal) and Wolverine's codegen
default (public concretes preferred) collide. We must choose which gives.

## Decision

Keep the adapters `internal sealed`, and opt the affected ports into
Wolverine service location in the Api's `UseWolverine`:

    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();

This keeps `ServiceLocationPolicy.NotAllowed` as the default everywhere
else (accidental opaque registrations still fail loudly), inlines all
other handler dependencies, and routes only these named internal-typed
ports through the container. New internal-typed handler dependencies in
later phases are added to this list; a missing entry throws at startup,
so the omission is self-correcting rather than silent.

## Considered and rejected

- **Make the adapters `public`.** Rejected: it trades the project's
  encapsulation rule (adapters are internal implementation details) for a
  codegen micro-optimization, and exposes concrete Infrastructure types on
  the assembly's public surface. The per-dispatch service-resolve cost is
  negligible next to an HTTP call plus a database write.
- **`ServiceLocationPolicy.AllowedButWarn` / `AlwaysAllowed` wholesale.**
  Rejected for now: restores the 5.x fallback globally. `AllowedButWarn`
  logs a warning for every service-located dependency (noise, since
  internal adapters are the norm here); `AlwaysAllowed` silences the
  safety net entirely, so a genuinely accidental opaque registration would
  no longer fail. The targeted allow-list preserves the strict default and
  documents intent. Revisit if the list becomes unwieldy.
- **`TypeLoadMode.Static` (commit generated code).** Rejected: generated
  code would compile inside the Api assembly and see internal types, but
  it requires regenerating committed code on every handler change — a
  heavier workflow than this project wants, and at odds with the
  runtime-compilation setup.

## Consequences

Adapters stay internal; the Wolverine config in the Api carries a small,
explicit allow-list of internal-typed ports that grows with the handler
roster. An agent that "fixes" `InvalidServiceLocationException` by making
an adapter public has violated the encapsulation rule — the correct fix
is an entry in `AlwaysUseServiceLocationFor<T>`.
