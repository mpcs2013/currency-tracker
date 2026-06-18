# HTTP API (Phase 10)

> All endpoints are currently UNAUTHENTICATED. Authentication/authorisation
> land in Phase 11; do not expose this API on a public ingress before then.

## GET /api/v1/rates/latest

The most recent persisted snapshot of rates for a base currency.

- Query: `base` — 3-letter uppercase ISO 4217 code (e.g. `USD`). Required.
- `200 OK` — JSON array of `{ base, quote, rate, asOf }`.
- `400 Bad Request` — `application/problem+json` when `base` is missing or
  malformed (validation middleware).
- `404 Not Found` — `application/problem+json` when no snapshot exists for
  the base.
- Caching: served from Redis when warm; ~5-minute TTL with ±10% jitter;
  the key (`rates:latest:{base}`) is evicted on each successful ingest, so a
  read after an ingest repopulates with fresh data.

## GET /api/v1/rates/history

The rate for a base/quote pair over a bounded date range.

- Query: `base`, `quote` (distinct ISO 4217 codes), `from`, `to` (dates,
  `from <= to`, range <= 366 days). All required.
- `200 OK` — JSON array of `{ asOf, rate }`, ascending by date. An empty
  range returns `200 []` (an empty history is a valid answer, not a 404).
- `400 Bad Request` — malformed codes, `base == quote`, inverted or
  over-long range.
- Caching: none — the key space (base × quote × range) is large and reuse
  is low. See `docs/caching.md`.
