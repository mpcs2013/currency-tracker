# 0010 — OIDC/JWT bearer auth: Keycloak default, Entra ID alternative

- **Status:** Accepted
- **Date:** 20.06.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0001-stack-choices.md (auth stack choice), 0009-postgres-image-pin.md
  (container image pinning, applied here to Keycloak), Phase 4 (ICurrentUser port),
  Phase 7 (Aspire resource + env-var injection)

## Context

The API's read and write slices (Phases 9–10) ship unauthenticated, deferred
to Phase 11. ADR 0001 chose OIDC with a JWT bearer in the API, Keycloak locally
and Entra ID in Azure. This ADR records *how* that lands and what it costs.

## Decision

- Validate bearer tokens with `Microsoft.AspNetCore.Authentication.JwtBearer`
  (the plain package), configured from `Authentication:Authority` and
  `Authentication:Audience`. The API takes no Keycloak- or Entra-specific
  dependency: switching providers is a config change.
- Keycloak runs as an Aspire resource (`Aspire.Hosting.Keycloak`, currently a
  preview package on the 13.4.x line) with a named data volume, a pinned image
  tag (per 0009), and a realm imported via `WithRealmImport`.
- `HttpContextCurrentUser : ICurrentUser` lives in
  `Infrastructure/Security/` (the AGENTS.md ports table's stated home). To read
  `HttpContext`/`ClaimsPrincipal` it adds a `Microsoft.AspNetCore.App`
  FrameworkReference to Infrastructure — Infrastructure's first ASP.NET surface.
- `ClockSkew = 30s`; `RequireHttpsMetadata = true` outside `Development`;
  `MapInboundClaims = false`.

## Considered and rejected

- **A Keycloak-specific client helper (e.g. `AddKeycloakJwtBearer`).** Rejected:
  it couples the API to the provider and breaks the one-config-change swap.
- **Putting the adapter in the Api project instead of Infrastructure.** A
  legitimate alternative (guardrail §2 calls auth a boundary/edge concern) that
  keeps Infrastructure framework-free, but it contradicts the AGENTS.md ports
  table. Left as Infrastructure here; revisit if the Worker footprint cost bites.

## Consequences

- The API is IdP-agnostic; Entra ID (11.11) is `appsettings.Azure.json` + no code.
- Infrastructure (and, transitively, the Worker that references it) now pulls the
  ASP.NET shared framework. Flagged as a footprint cost; see 11.6.