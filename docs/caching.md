# Caching (Phase 10)

The cache sits in front of *reads*, behind the Application `ICacheService`
port (Phase 4). Redis is the adapter (`RedisCacheService`, Phase 10.2); the
in-memory fake backs the unit tests. Handlers never see Redis.

## Key convention

Keys are built only from `CacheKeys` (`Application/Caching/CacheKeys.cs`) so
readers and invalidators agree byte-for-byte. Format:

    rates:latest:{base}      e.g. rates:latest:USD

Keys carry **only non-sensitive identifiers** (currency codes). No user
identity, no tokens, no PII (see the security review, Phase 10.11). When
per-user data is cached in a later phase, derive a stable non-reversible key
— never embed a raw identifier or token.

## TTL and jitter

TTL is a relative `TimeSpan` chosen by the caller (latest-rates: 5 minutes).
The adapter applies ±10% jitter to the stored expiry so simultaneously-set
keys don't expire together and stampede Postgres. Jitter is the adapter's
concern, invisible to callers.

## Coherence

`GetLatestRatesHandler` populates `rates:latest:{base}` on a cache miss.
`IngestDailyRatesHandler` removes the same key after each successful upsert
(Phase 10.6) so the next read repopulates with fresh data. Eviction targets
the **exact** key — never a `KEYS`/`SCAN` pattern sweep (Phase 10.11).

## What is not cached

`GetRateHistoryQuery` (Phase 10.7) is uncached: its key space (base × quote ×
date range) is large and its per-key reuse is low. A cache earns its keep
when keys are few and reads repeat.

## In Azure (Phase 14.45)

The adapter above is unchanged. What changed is how the *connection* is made —
which is the point of the `ICacheService` port: the query slice never noticed.

- **Azure Managed Redis, not Azure Cache for Redis.** The retired product's
  defaults are inverted here. Access keys are **disabled at creation**
  (`access_keys_authentication_enabled = false`, an invariant in
  `modules/redis`), so there is no key path — not even a temporary one — and the
  first connection the application ever makes must present a Microsoft Entra
  access token.
- **Port 10000**, read from the resource by `modules/redis`' output and composed
  into the connection string by Terraform (14.42). Not 6380. A `6380` anywhere
  in this repository means something reconstructed a connection string instead
  of reading the resource.
- **TLS is set explicitly**, not inferred from the hostname: `client_protocol =
  "Encrypted"` refuses a plaintext attempt, and being explicit turns that into a
  readable TLS error rather than a connection reset.
- **RESP3.** Under RESP2 the client opens a second, subscription connection that
  the token extension cannot re-authenticate, so the server drops it at every
  token expiry and the client restores it — harmless, and it emits
  `MicrosoftEntraTokenExpired` into the cache's error metrics forever.
- **Authentication is `Microsoft.Azure.StackExchangeRedis`'
  `ConfigureForAzureWithTokenCredentialAsync`**, which sets the connection user
  to the identity's object id and installs the re-authentication timer. A raw
  token in `ConfigurationOptions.Password` is rejected by Managed Redis; that
  shape is correct only for the retired product. ADR 0017.
- **`AddStackExchangeRedisCache` is wired through `ConnectionMultiplexerFactory`**
  in the Azure path, because the extension above is asynchronous and the factory
  is the only seam that can be awaited. The local path still uses
  `Configuration` and is byte-for-byte Phase 10's.
- **Only the Api holds a cache grant.** 14.24's Managed Redis access-policy
  assignment targets the Api principal alone — the Worker never opens a cache
  connection, and grants follow code. Both hosts receive
  `ConnectionStrings__cache` because `AddInfrastructure()` fail-fasts on its
  absence in either; in the Worker the factory is simply never invoked.

Readiness is what proves all of this: `/health/ready` (Phase 13.B) checks
Postgres and Redis, so it could not return 200 from Azure until the tokens
worked. `AbortOnConnectFail = false` changes reconnection behaviour, not command
behaviour — an operation against an unavailable multiplexer still throws, and
the readiness check deliberately has no `catch`.