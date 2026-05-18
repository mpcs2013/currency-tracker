# 0001 — Stack choices

- **Status:** Accepted
- **Date:** 2026-05-18
- **Authors:** @your-handle

## Context

This is a solo learning-by-doing project. The goals are: practise Clean
Architecture on a non-toy domain, get hands-on with the current .NET LTS,
ship something deployable to Azure end-to-end. Constraints: one developer,
working with AI agents, no team to spread the cognitive load across.

## Decision

| Concern               | Choice                          | Notes                                                  |
| --------------------- | ------------------------------- | ------------------------------------------------------ |
| Runtime               | .NET 10 LTS                     | Support window through November 2028.                  |
| Language              | C# 14                           | Latest, ships with .NET 10.                            |
| Messaging / mediation | **WolverineFx**                 | Convention-based handlers, source-generated dispatch.  |
| Validation            | FluentValidation                | Composes with Wolverine middleware cleanly.            |
| Persistence           | EF Core 10 + Postgres           | Postgres because it is the right default for new work. |
| Cache                 | Redis (StackExchange.Redis)     | Cache-aside for read-heavy queries.                    |
| Local orchestration   | .NET Aspire                     | Polyglot resources, dashboard, OTLP for free.          |
| Auth                  | Keycloak (default), Entra ID (alt) | OIDC; JWT bearer in the API.                        |
| Observability         | OpenTelemetry                   | OTLP → Aspire dashboard locally, App Insights in cloud.|
| Tests                 | xUnit v3                        | v3 is the current major; different discovery from v2.  |
| Cloud                 | Azure (Container Apps + ACR)    | Phase 14.                                              |

## Considered and rejected

- **MediatR** instead of WolverineFx. Rejected: Wolverine offers richer
  out-of-the-box features (durable messaging, scheduled messages, transactional
  outbox, source-generated dispatch) that MediatR would need bolted on. For a
  learning project where the messaging layer is part of what's being learned,
  the heavier-but-coherent option wins.
- **MassTransit** instead of WolverineFx. Rejected for this project: excellent
  for distributed systems but heavier than needed for a single-service backend.
  Worth revisiting if the project grows multiple services.
- **Dapper** or **raw ADO.NET** instead of EF Core. Rejected: EF Core's value
  converters, change tracking, and tooling are part of the curriculum here. A
  Dapper-based slice could be revisited in a "performance" phase if needed.
- **Docker Compose** instead of Aspire for local orchestration. Rejected:
  Aspire gives the dashboard and OTLP wiring for free, and is a thing worth
  learning. Compose is the fallback if Aspire becomes a blocker.
- **SQLite** instead of Postgres for local dev. Rejected: parity with prod
  matters more than the slight reduction in local-dev friction. Aspire makes
  the Postgres container start with `dotnet run` anyway.

## Deferred

- React + TypeScript frontend. Phase 16, only if there's time/appetite. The
  API has to stand alone first.
- Multi-region deployment, blue/green, read replicas — none of those are
  appropriate at this scale and would distract from the core learning goals.

## Consequences

- Agents will sometimes suggest MediatR-style markers because that's the most
  common pattern in their training data. Reject those suggestions and point
  at this record.
- The `Directory.Packages.props` constraint applies to every change: the
  versions of the listed libraries are pinned and only bumped intentionally.
- A second decision record will land when the first significant alternative
  is considered (e.g. Entra ID actually replacing Keycloak in production).