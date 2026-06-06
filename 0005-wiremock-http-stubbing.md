# 0005 — WireMock.Net for stubbing external HTTP in integration tests

- **Status:** Accepted
- **Date:** 2026-05-28
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** [`0004-ef-core-persistence.md`](./0004-ef-core-persistence.md)

## Context

Phase 9 lands the first integration with an external HTTP provider
(Frankfurter). The Phase 9.9 integration test must exercise the full
Infrastructure ingestion stack — the typed `FrankfurterClient`, its Polly
resilience pipeline, the `FrankfurterExchangeRateProvider` adapter, and
the Phase 8 EF Core repository against a real Postgres — without depending
on the live Frankfurter API.

The question is *how to fake the external HTTP server* in that test.

## Decision

Use `WireMock.Net` (test-only) to stand up a local HTTP server in the
integration test. The real `FrankfurterClient` is pointed at the WireMock
server via a `Frankfurter:BaseUrl` configuration override; WireMock
returns canned Frankfurter-shaped JSON and can model scenario state
(return 500 once, then 200) to exercise the resilience pipeline's retry.

`WireMock.Net` is referenced **only** by
`tests/CurrencyTracker.Infrastructure.IntegrationTests`. It must never
appear in a `src/` project's `.csproj`. Version pinned in
`Directory.Packages.props`.

## Considered and rejected

- **Hitting the live Frankfurter API.** Rejected: network-dependent
  (CI fails when the provider is slow/down), rate-limited (retries could
  hammer a free public API), and non-deterministic (rates change daily).
  Tests must be hermetic.
- **A hand-rolled `HttpMessageHandler` stub.** Rejected for the
  integration tier: it bypasses the real `HttpClient` pipeline (typed
  client config, resilience handler, socket round-trip) — the very thing
  the integration test exists to exercise. (It is the right tool for the
  9.4 *unit* tests of the adapter's mapping, where the HTTP pipeline is
  out of scope; those use a local `StubHttpMessageHandler`.)
- **A second Testcontainers container running WireMock's Docker image.**
  Rejected as unnecessary weight: the in-process `WireMockServer.Start()`
  is faster, simpler, and gives the test direct control over scenarios.

## Consequences

The integration test exercises the entire Infrastructure stack with the
HTTP transport real and the external server faked — the highest-fidelity
test that's still hermetic. The cost is one test-only top-level
dependency. An agent that reaches for `WireMock.Net` in a `src/` project
to "stub something at runtime" has misused it; it is a test fake, full
stop.
