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