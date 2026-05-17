---
status: "accepted"
date: 2026-05-17
decision-makers: ["@mpcs2013"]
consulted: []
informed: []
---

# Use Clean Architecture for the CurrencyTracker solution

## Context and Problem Statement

CurrencyTracker is a small service with a clear domain (foreign-exchange rates, alert rules) and several cross-cutting concerns (HTTP, messaging, persistence, caching, scheduling, auth, observability). Without an explicit architectural style, the temptation is to put every concern in the same place — a single project with controllers calling EF Core directly, business rules sprinkled across services and DTOs, and tests that have to spin up a database to exercise a price-comparison rule.

How should the solution be structured so that the domain logic is testable in isolation, the cross-cutting concerns can be swapped without rewriting domain code, and the dependency direction is enforced by tooling rather than memory?

## Considered Options

- **Clean Architecture** (4 layers: Domain → Application → Infrastructure → Api/Worker), dependency direction enforced by NetArchTest.
- **Vertical slice architecture** — features organised as folders, each containing its own request/handler/persistence, no shared "domain" layer.
- **Anaemic-domain CRUD** — controllers calling a service layer calling EF Core, with DTOs as the only types and no rich domain types.

## Decision Outcome

Chosen option: **Clean Architecture**, because:

- The Domain layer is pure C# with zero NuGet dependencies, which makes domain rules testable as pure functions — no test fixtures, no database, no HTTP.
- Cross-cutting concerns (persistence, HTTP, caching, auth) live behind ports defined in Application and implemented in Infrastructure. Swapping Redis for Memcached, or EF Core for Dapper, is a per-adapter change, not a sprawling diff.
- The dependency direction is enforceable: a NetArchTest unit test fails the build if anything in Domain references anything outside Domain. The build *is* the architecture diagram.
- This service is small enough that the Clean Architecture overhead (more projects, more wiring) is paid once at scaffolding (Phase 2) and amortises across every subsequent feature.

### Dependency rules

```text
Domain        ← pure C#, zero outside refs
   ▲
Application   ← ports, handlers, CQRS messages (Wolverine convention-based)
   ▲
Infrastructure ← EF Core, Redis, HTTP clients, identity adapters
   ▲
Api / Worker  ← composition roots (HTTP host & background host)
   ▲
AppHost (Aspire) ← local orchestration only — never referenced by app code