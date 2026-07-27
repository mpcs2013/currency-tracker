# 0017 — Passwordless Azure data-plane auth: Entra tokens from the workload's own managed identity

- **Status:** Accepted
- **Date:** 27.07.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0004-ef-core-persistence.md (the EF Core/Npgsql stack this
  re-authenticates), 0007-redis-distributed-cache.md (the cache client this
  re-authenticates), 0014-OIDC-posture.md (the *deploy* identity — a distinct
  identity set from the workload identities here), 0010-oidc-jwt-auth.md (the
  *caller's* token — a third, unrelated concern), Phase 14.C (the hardening that
  removed both passwords), Phase 14.E (14.44/14.45, the client half)

## Context

Phase 14.C created both data planes with their credential paths switched off:
`modules/postgres` sets `password_auth_enabled = false`, and `modules/redis`
sets `access_keys_authentication_enabled = false` on Azure Managed Redis from
birth. There is no password to store and no interim key path to lean on — the
very first connection either application makes in Azure must present a Microsoft
Entra access token.

Both wire protocols still expect a password. PostgreSQL has a password message;
Redis has `AUTH`. "Passwordless" therefore does not mean a missing credential —
it means a short-lived token, fetched from the instance metadata endpoint and
scoped to the target resource, is what goes in the password slot. Because tokens
expire in roughly an hour and a connection pool outlives them, both clients need
a refresh story.

Two vendor extensions exist for exactly this, and the alternative is hand-rolled
token caching in two places.

## Decision

- **The application authenticates to its Azure data planes with Entra tokens
  acquired from its own system-assigned managed identity**, gated by one
  configuration switch, `Azure:UseManagedIdentity`, supplied in Azure as the
  `Azure__UseManagedIdentity` environment variable (14.43) and defaulted to
  `false` in `appsettings.json`.
- **Two new top-level packages, both referenced by `Infrastructure` only:**
  - `Azure.Identity` (14.44) — `TokenCredential` and its internal token cache.
  - `Microsoft.Azure.StackExchangeRedis` (14.45) — the token extension for
    Azure Managed Redis, including proactive re-authentication before expiry.
- **`TokenCredential` is the abstraction; no port is introduced.** No
  `ISecretProvider`, no `ITokenSource`, no `IConnectionFactory` in Application.
  The Azure SDK ships an abstract class with a test-friendly surface, and a
  nine-line in-memory fake covers the tests.
- **Postgres: `NpgsqlDataSourceBuilder.UsePasswordProvider`, both delegates.**
  `ApplicationDataSource.Create` attaches a synchronous *and* an asynchronous
  provider scoped to `https://ossrdbms-aad.database.windows.net/.default`. The
  synchronous one is implemented rather than throwing — see Consequences.
- **Redis: `ConfigureForAzureWithTokenCredentialAsync` behind
  `RedisCacheOptions.ConnectionMultiplexerFactory`.** The extension is
  asynchronous, and the factory is the only `AddStackExchangeRedisCache` seam
  that can be awaited. Options are shaped for Azure Managed Redis: TLS on,
  RESP3, and the port read from the resource (10000, not the retired product's
  6380).
- **`DefaultAzureCredential`, not `ManagedIdentityCredential`.** The chain costs
  a few hundred milliseconds once and buys the ability to run a host locally,
  `az login`-ed, against real UAT infrastructure — the diagnostic you want the
  first time a deployed revision cannot connect.
- **The application stays vault-unaware.** No Key Vault configuration provider,
  no vault client. Container Apps resolves secret references before the process
  starts (see `docs/configuration.md`).

## Considered and rejected

- **`UsePeriodicPasswordProvider` with a fixed refresh interval.** The older
  Npgsql API. It adds a second cache in front of the one already inside
  `Azure.Identity`, with a hardcoded interval free to drift from the real token
  lifetime. Npgsql's documentation now leads with `UsePasswordProvider` for
  precisely the cloud-provider-caches case. Rejected.
- **A hand-rolled `AccessToken` field with a `DateTimeOffset` expiry check.**
  Same defect, written by us, and the refresh timer is where the bugs live.
  Rejected.
- **Throwing `NotSupportedException` from the synchronous password provider**,
  as the Npgsql documentation suggests. Sound advice for connections you open
  yourself; this codebase hands the same data source to Wolverine's durability
  subsystem, which owns connection lifecycles, leader election and heartbeat
  agents that this project does not control. Rejected — the bet's downside is a
  `NotSupportedException` at 3am.
- **`ManagedIdentityCredential`.** Faster and less chatty in a container, and it
  cannot authenticate a developer. Rejected for now; narrowing to it later is a
  one-line change with a known cost.
- **`options.Password = token` for Redis.** The correct answer for the *retired*
  Azure Cache for Redis and the single most likely wrong turn here. Azure Managed
  Redis rejects a raw token in the password slot; the extension sets the
  connection user to the identity's object id and installs re-authentication.
  Rejected because it does not work.
- **RESP2 (the default protocol).** Opens a second, subscription connection that
  cannot be re-authenticated, so the server drops it at every token expiry and
  the client restores it. Functionally fine; it emits `MicrosoftEntraTokenExpired`
  into the cache's error metrics forever, which trains you to ignore the metric
  14.52 will alert on. Rejected for one line of configuration.
- **An in-process Key Vault configuration provider
  (`Azure.Extensions.AspNetCore.Configuration.Secrets`).** A legitimate pattern,
  and a second path to a value the platform already resolved — plus a startup
  round trip and a credential chain to debug on a laptop. Rejected; the platform
  path wins because it fails *before* the process exists.
- **A separate `StackExchange.Redis` version pin.** `ConfigurationOptions`,
  `RedisProtocol` and `ConnectionMultiplexer` arrive transitively, as they have
  since Phase 10 via `Microsoft.Extensions.Caching.StackExchangeRedis`. Pinning
  both creates a compatibility pair to maintain by hand. Rejected.

## Consequences

- **No credential exists at rest anywhere in the system.** Nothing to rotate,
  nothing to leak, nothing for `gitleaks` to find. A stolen token is useless in
  about an hour.
- **Both hosts share one authentication implementation.** `ApplicationDataSource`
  has two callers — EF Core's registration and the Worker's Wolverine outbox —
  and is still a static factory, because two *calls* are not two *concepts*.
- **The synchronous provider is load-bearing, not defensive.** Removing it
  converts any synchronous connection open inside Wolverine's durability
  subsystem into a hard failure. Keep both delegates.
- **The local path is untouched.** With the switch off, the connection string is
  used exactly as supplied and the cache registration is byte-for-byte Phase
  10's. Aspire, the Testcontainers suites and the AppHost smoke tests needed no
  edits, which is the regression evidence that matters.
- **Two packages, one layer.** `Api` and `Worker` gained no package references;
  both reach Azure through `AddInfrastructure()`. The dependency-direction test
  in `Architecture.Tests` is unaffected — it inspects project references.
- **`DefaultAzureCredential` resolves a different principal locally than in the
  container.** "It works on my machine against real UAT" proves your Entra
  account is a Postgres administrator, not that the app's identity is. Check
  `az account show --query user.name` when interpreting a local success.
- **Verify what NuGet actually resolved** for the transitive
  `StackExchange.Redis` before merging a version bump:
  `dotnet list src/CurrencyTracker.Infrastructure package --include-transitive`.

## Notes

- The grants these tokens exercise are not in this ADR: the Postgres Entra
  administrator registrations and the Managed Redis access-policy assignment
  live in `infra/terraform/modules/role-assignments` (14.24), which is
  deliberately readable top to bottom as the least-privilege ledger.
- The Redis grant is **Api-only**, on purpose — the Worker never opens a cache
  connection. `ConnectionMultiplexerFactory`'s laziness is what makes that
  asymmetry survivable rather than a boot failure in the Worker.
- Package versions are pinned in `Directory.Packages.props`; a `Version=`
  attribute in a `.csproj` is a Central Package Management violation the build
  will not catch for you.
