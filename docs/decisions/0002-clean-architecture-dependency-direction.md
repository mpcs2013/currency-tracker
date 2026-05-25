# 0002 — Clean Architecture dependency direction

- **Status:** Accepted
- **Date:** 2026-05-20
- **Authors:** @your-handle
- **Supersedes:** —
- **Related:** [`0001-stack-choices.md`](./0001-stack-choices.md),
  [`../agents/cross-layer-guardrails.md`](../agents/cross-layer-guardrails.md)

## Context

Phase 2 of the build plan ("Solution skeleton") creates five empty .NET
projects: `Domain`, `Application`, `Infrastructure`, `Api`, `Worker`. The
question is not *which* projects exist — that's settled by the build
plan — but **what the project-reference graph between them looks like
and how it stays correct over the project's lifetime**.

The answer matters now (not after Phase 2 has scaffolded everything)
because:

- Reference direction encodes the Clean Architecture contract. Once
  business code lands in the wrong layer, refactoring back across the
  layers costs days; while the projects are empty, the entire graph can
  be redrawn in twenty seconds.
- This is the foundational guardrail every later phase rests on. Phase
  5's Wolverine bootstrap, Phase 8's EF Core, Phase 9's HTTP endpoints,
  Phase 11's auth — each of these has a "where does this code go" question
  whose answer falls out of the dependency contract recorded here.
- Solo development plus AI agents amplifies the risk of accidental
  shortcuts. An agent that hasn't read this ADR will plausibly suggest a
  `using CurrencyTracker.Infrastructure;` in Application "because it's
  simpler". The ADR is the paper trail the next session reads to know
  the rule wasn't an accident.

## Decision

The solution follows Clean Architecture's dependency-direction rule:

```
Domain  ←  Application  ←  Infrastructure  ←  (Api | Worker)
```

Concretely:

| Project          | References                              | NuGet packages allowed                                                   | Composition root |
| ---------------- | --------------------------------------- | ------------------------------------------------------------------------ | ---------------- |
| `Domain`         | (none)                                  | (none)                                                                   | No               |
| `Application`    | `Domain`                                | Wolverine, FluentValidation, abstractions; no I/O packages               | No               |
| `Infrastructure` | `Application`, `Domain`                 | EF Core, Redis client, `HttpClient`, OpenTelemetry exporters             | No               |
| `Api`            | `Infrastructure`, `Application`, `Domain` | ASP.NET Core, auth handlers, anything needed to host HTTP              | **Yes** (HTTP)   |
| `Worker`         | `Infrastructure`, `Application`, `Domain` | Hosted services, schedulers, anything needed to host background jobs   | **Yes** (jobs)   |

Two composition roots is the current contract: `Api` for HTTP, `Worker`
for background jobs. **`Api` and `Worker` do not reference each other**;
they communicate via the message bus (Phase 5+) or, when distributed,
via HTTP.

The contract is enforced two ways:

- **`Directory.Build.props` + `Directory.Packages.props`** keep package
  references and version-pinning consistent across the solution; the
  Domain project specifically declares zero packages.
- **`NetArchTest.Rules` architecture tests** (Phase 2.2 onward) fail the
  build when a `using` slips through that violates the graph. The tests
  live in `tests/CurrencyTracker.Architecture.Tests` and assert each
  forbidden direction by name (`Application_references_only_Domain`,
  `Infrastructure_references_only_Application_and_Domain`,
  `Api_does_not_reference_Worker`, `Worker_does_not_reference_Api`,
  `Src_projects_do_not_reference_test_packages`).

The operational rules — what each layer is and isn't allowed to do at
the code level (no business rules in Api; no I/O code in Application;
no domain logic in Infrastructure) — live in
[`docs/agents/cross-layer-guardrails.md`](../agents/cross-layer-guardrails.md).
That document is the agent-facing rule sheet; this ADR is the rationale
behind it.

## Considered and rejected

- **Onion / Hexagonal / Ports-and-adapters** as the *named* architecture
  instead of Clean. Rejected as a naming choice, not a substance choice:
  all three describe the same dependency-inversion shape. "Clean
  Architecture" is the most common phrase in current literature and the
  one most agents in this project's training-data window will recognise.
- **A single `Application` project that also contains the EF Core
  adapters** ("CQRS lite", popular in Microsoft-eShop-style samples).
  Rejected: blurring Application and Infrastructure makes it impossible
  to test Application against in-memory port implementations without
  pulling EF Core onto the test classpath. Phase 6 onward depends on
  this separation.
- **A `Shared` or `Common` project depended on by everyone.** Rejected:
  it inevitably grows into a junk drawer that erodes the dependency
  contract from the inside. If two layers need the same primitive, it
  belongs in `Domain`; if Application and Infrastructure both need an
  abstraction, the abstraction belongs in `Application` and the
  implementation in `Infrastructure`.
- **A separate `Contracts` project** for inter-service DTOs. Rejected
  *for now*: there's only one HTTP service in this build. The moment a
  second consumer enters the picture (a frontend with shared TypeScript
  generation, a second backend service, a public OpenAPI contract), a
  `Contracts` project is the right next step and will get its own ADR.
- **Letting `Api` reference `Worker` (or vice versa) "for symmetry"**.
  Rejected: that's the kind of edge that compiles fine in Phase 2 and
  becomes a deployment-coupling disaster by Phase 14 (Container Apps
  hosts the two as separate revisions). Communication between the
  composition roots goes via messages.
- **Enforcing the contract only through code review** (no architecture
  tests). Rejected: solo development with AI agents means code review is
  a single pair of eyes against an unbounded number of agent-generated
  diffs. The compile-time/IL-level check is the only enforcement that
  scales.

## Deferred

- A third composition root. The next likely candidate is a CLI host for
  one-off operational commands, or a Functions app for event-driven work
  the Worker doesn't fit. Either is ADR-worthy when it lands.
- Splitting `Application` along feature/slice boundaries (vertical-slice
  architecture). Worth revisiting if the project grows past ~30 use cases;
  the current Phase-by-Phase scope doesn't approach that.
- A separate `Domain.Events` project. The current build plan keeps
  domain events inside `Domain`. Splitting them out is only useful if a
  consumer outside the solution (a separate service, a public event
  schema) needs to reference the events without the rest of Domain.

## Consequences

The dependency-direction rule is the single most-cited guardrail in
agent prompts, ADRs, and architecture tests. Every later phase assumes
it: Wolverine handlers in Application reference `Domain` types only;
EF Core adapters in Infrastructure implement `Application` ports;
HTTP endpoints in Api compose what's below them and add nothing of
their own. When an agent proposes "just this once, put it in Domain"
(or Application, or anywhere else), the correct response is to point
at this ADR. The architecture tests catch the rest.

The cost is two pieces of friction that show up consistently. First,
every new top-level NuGet dependency has to be assigned to a specific
layer with an explicit rationale; in practice this means a one-line
entry in `Directory.Packages.props` and an ADR (Phase 1 onward) when
the choice is non-trivial. Second, "convenience" cross-references that
agents propose — Application reaching for a `DbContext`, Api computing
business rules inline — fail the architecture tests and produce CI red
that the next person has to understand before they can merge. Both
costs are intentional. Drift here is the failure mode this ADR exists
to prevent.
