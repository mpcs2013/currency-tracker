# Authentication & authorisation (Phase 11)

The API authenticates callers with OIDC-issued **JWT bearer tokens**. Locally
the identity provider is **Keycloak** (an Aspire resource); in Azure it is
**Entra ID**, reached by a config swap with no code change (see
`appsettings.Azure.json`). The API validates issuer, audience, lifetime, and
signing key, with a 30-second clock skew. See ADR `0010-oidc-jwt-auth.md`.

> The read and admin endpoints are **authenticated** as of Phase 11. Phase 9
> and Phase 10 shipped them open and recorded that auth would land here.

## Endpoints and what they require

| Endpoint                      | Requires                         |
| ----------------------------- | -------------------------------- |
| `GET /api/v1/rates/latest`    | any authenticated caller         |
| `GET /api/v1/rates/history`   | any authenticated caller         |
| `POST /admin/ingest`          | the `admin` role (policy "admin")|
| `/health`, `/alive`           | anonymous (liveness/readiness)   |

## Getting a token via curl (local Keycloak, dev only)

Keycloak's `currency-tracker-api` client is a **public client with direct
access grants enabled** purely so this resource-owner-password (ROPC) flow
works in development. **ROPC is never a production pattern** — in Azure,
tokens come from Entra ID via the normal authorization-code/OBO flows.

Run the AppHost (`dotnet run --project src/CurrencyTracker.AppHost`) and note
the Keycloak port (8080) and the Api port from the dashboard. Then:

```bash
# 1) A user-role token (read access):
USER_TOKEN=$(curl -s \
  "https://localhost:8080/realms/currency-tracker/protocol/openid-connect/token" \
  -d "grant_type=password" \
  -d "client_id=currency-tracker-api" \
  -d "username=tester" \
  -d "password=Passw0rd!" | jq -r .access_token)

# 2) Call the read endpoint with it:
curl -s -H "Authorization: Bearer $USER_TOKEN" \
  "https://localhost:<api-port>/api/v1/rates/latest?base=USD"

# 3) An admin-role token (admin access):
ADMIN_TOKEN=$(curl -s \
  "https://localhost:8080/realms/currency-tracker/protocol/openid-connect/token" \
  -d "grant_type=password" \
  -d "client_id=currency-tracker-api" \
  -d "username=admin-user" \
  -d "password=Passw0rd!" | jq -r .access_token)

# 4) Trigger ingestion (admin only):
curl -s -X POST -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  "https://localhost:<api-port>/admin/ingest" \
  -d '{"baseCurrency":"USD","asOf":"2026-06-20"}'
  -d '{"baseCurrency":"USD","asOf":"2026-06-20"}'
  
## Switching to Entra ID (Azure)

The API is IdP-agnostic: it validates whatever `Authentication:Authority` and
`Authentication:Audience` point at. `appsettings.Azure.json` re-points both at
Entra ID, so running with `ASPNETCORE_ENVIRONMENT=Azure` swaps the provider
with **no code change**:

- `Authority` → `https://login.microsoftonline.com/<tenant-id>/v2.0`
- `Audience`  → the API app-registration client id (or `api://<client-id>`)

Two Entra specifics worth knowing:

- **Roles still land in the `roles` claim.** Configure **app roles** named
  `user`/`admin` on the API's app registration; Entra emits them in the same
  `roles` claim the API already reads (`RoleClaimType = "roles"`), so the
  `admin` policy works unchanged.
- **`sub` is a pairwise identifier, not a GUID.** Entra's `sub` won't parse as
  a `Guid`, so `HttpContextCurrentUser.UserId` is `null` for Entra callers —
  which is the Phase 4 contract working as designed (a non-parseable `sub` is a
  value, never a crash). `IsAuthenticated` stays `true`. (If a stable per-user
  GUID is ever needed, Entra's `oid` claim carries it — a future, ADR-worthy
  change to the adapter, not a Phase 11 one.)  
  
## Security posture (Phase 11.12 review)

- **Audience strictly validated.** `ValidateAudience = true`,
  `ValidAudience = currency-tracker-api`; issuer, lifetime, and signing key
  validated; clock skew clamped to 30s (`Program.cs`, 11.5). A wrong-audience
  token is rejected (11.9).
- **Secure-by-default.** `RequireAuthorizeOnAll()` protects every Wolverine
  endpoint; `/admin/ingest` additionally requires the `admin` role (policy).
  Health endpoints (`/health`, `/alive`) are intentionally anonymous (11.7/11.8).
- **No token logging.** No log statement emits the `Authorization` header or a
  raw token; framework logging is header-free. Forward rule: Phase 13's Serilog
  PII-redaction enricher must strip `Authorization` and token-shaped fields.
- **No JWT in URLs.** Tokens are accepted in the `Authorization` header only;
  no endpoint or doc passes a token in a query string (OWASP API8).
- **No JWT in cache keys.** Cache keys come only from `CacheKeys`
  (`rates:latest:{base}`) — currency codes, never identity (reaffirms 10.11).
- **Metadata over https outside Development.** `RequireHttpsMetadata` is true
  outside `Development` (11.4).
- **401/403 are ProblemDetails.** Auth failures return
  `application/problem+json` with a `traceId`, via `UseStatusCodePages` over
  the Phase 6 funnel — no token echoed in the error body.