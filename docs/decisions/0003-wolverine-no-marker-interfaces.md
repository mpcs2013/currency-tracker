# 0003 — Wolverine convention-based handler discovery; no marker interfaces

- **Status:** Accepted
- **Date:** 2026-05-24
- **Authors:** Marco Silva

## Context

Phase 5 introduces WolverineFx as the in-process message bus. WolverineFx
discovers handlers by scanning the application assembly for public types
named `*Handler` (or carrying `[WolverineHandler]`, or implementing
`IWolverineHandler`), then inspecting each `Handle` / `Consume` method's
parameter list to bind it to a message type. No marker interface on the
message is required; the parameter *type* is the contract.

The most common alternative pattern in .NET — MediatR's
`IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>` — predates
Wolverine by years and dominates training data for AI coding assistants.
Agents asked to "add a message and handler" routinely propose the MediatR
shape even after the project has chosen Wolverine.

## Decision

| Concern              | Choice                                                    | Notes                                                                    |
| -------------------- | --------------------------------------------------------- | ------------------------------------------------------------------------ |
| Handler discovery    | **Convention-based on parameter type**                    | No `IRequest<T>`, no `ICommand`, no `IDomainEvent`, no `IMessage` marker. |
| Message location     | `src/CurrencyTracker.Application/Messaging/`              | One folder per slice when slices grow; one file per message until then.  |
| Handler-with-message | Same file as the message when the handler is small        | Splits only when the handler grows past ~80 lines.                       |
| Handler type shape   | `public static class` with a `public static` Handle method | Static unless the handler needs scoped DI to be injected on its instance. |

## Considered and rejected

- **MediatR's `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>`
  pattern.** Rejected: Wolverine doesn't need it. Adding a marker interface
  on top of Wolverine's convention discovery creates a parallel taxonomy
  (every message implements `IRequest<T>`, every handler implements
  `IRequestHandler<,>`) that fights the framework rather than helping it.
  The pattern's perceived benefit — "the type system tells me what's a
  message" — is also the type system telling Wolverine's source generator
  things it already knows from the parameter type.
- **An internal `IMessage` / `ICommand` / `IQuery` marker hierarchy.**
  Rejected: same reasoning. A marker interface that *only this codebase
  uses* signals intent but enforces nothing; the next handler an agent
  writes will either skip the marker (silently) or invent a new one.
  Convention plus reviewer attention is the cheaper enforcement.
- **Splitting messages and handlers into different folders by default.**
  Rejected: the file co-location optimises for the reader. When you open
  `PingQuery.cs` and see both the record and the handler, the slice's
  shape is immediately legible. Splitting is reasonable when a handler
  grows past ~80 lines (loading data, orchestrating cascading messages,
  emitting events); it isn't free when the handler is six lines.

## Consequences

Agents (and tired humans) will sometimes propose adding `IRequest<T>`
because that's what their training data is dense with, or propose
inventing an `IMessage` marker "to make the messaging surface explicit".
Reject both. The convention is what the framework reads; adding shadow
markers introduces drift between what compiles and what runs, and is
exactly the failure mode `AGENTS.md`'s "Don't" list and this ADR exist
to head off.