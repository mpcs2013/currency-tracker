# 0007 — Redis distributed cache via Microsoft.Extensions.Caching.StackExchangeRedis

- **Status:** Accepted
- **Date:** 07.06.2026
- **Authors:** Marco Silva
- **Supersedes:** —
- **Related:** 0001-stack-choices.md (Redis named as the cache),
  Phase 4 ICacheService, Phase 7.4 (cache resource)

## Context

Phase 4 defined the ICacheService port and an in-memory fake; Phase 7 stood
up a Redis container named `cache` in the AppHost. Phase 10 needs a real
cache client behind that port. The question is which client library
registers the IDistributedCache the Phase 10.2 adapter wraps.

## Decision

Use Microsoft.Extensions.Caching.StackExchangeRedis and register
IDistributedCache via AddStackExchangeRedisCache, configured from
IConfiguration.GetConnectionString("cache"). The package is referenced only
by CurrencyTracker.Infrastructure. The RedisCacheService adapter (10.2) is
its only consumer; nothing else references IDistributedCache or
StackExchange.Redis directly.

## Considered and rejected

- Aspire.StackExchange.Redis.DistributedCaching (AddRedisDistributedCache).
  Rejected for now: convenient (resolves by resource name, adds health
  checks + telemetry) but a larger dependency surface and an Aspire-coupled
  registration. The plain package's explicit GetConnectionString("cache")
  read matches the Phase 8 Postgres pattern and ports unchanged to Phase 14's
  Container-Apps deployment, where the connection string is an environment
  variable, not an AppHost reference.
- StackExchange.Redis directly (raw IConnectionMultiplexer). Rejected: more
  power than the cache-aside use case needs, and it would push connection
  lifecycle management into the adapter. IDistributedCache is the right
  abstraction level for get/set/remove with relative TTLs.
- Hardcoding the connection string. Rejected on the project's explicit
  no-list: breaks on dynamic AppHost ports and on Azure.

## Consequences

The cache client is a single Infrastructure-internal dependency configured
from the Aspire-injected connection string. An agent that adds the Aspire
client integration "for the health checks" has changed the decision — that's
a new ADR, not a drive-by. An agent that hardcodes the connection string has
violated the no-hardcoded-connection-strings rule.