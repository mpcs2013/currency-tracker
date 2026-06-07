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