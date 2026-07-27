# UAT environment values. Cost-leaning: single-zone, public access + firewall.
# Contains NO secrets — secrets resolve from Key Vault (Phase 14.E).
environment = "uat"
name_prefix = "ct-uat"

tags = {
  app        = "currencytracker"
  env        = "uat"
  owner      = "marco"
  costcentre = "currency-tracker"
}

# UAT trades resilience for cost: single-zone Postgres.
postgres_zone_redundant = false

# UAT is reachable publicly but firewalled to known ranges (tightened in 14.G).
enable_public_network_access = true

# The firewall those "known ranges" refer to. It did not exist until now, and a
# public Flexible Server with no rules denies everything — the Api's readiness
# probe hung on a dropped connect to 5432 and UAT never converged. Azure-internal
# is the only grain available: the Container Apps environment is external and
# egresses from a rotating pool of ~160 public IPs, so there is no address to
# pin. Password auth is off, so this opens a port, not an account.
postgres_allow_azure_services = true

# Environment VNet. Non-overlapping with PROD (10.30.0.0/16) so the two could
# be peered later without renumbering. Subnets are derived in modules/network.
vnet_address_space = ["10.20.0.0/16"]

# Burstable: cheapest tier that runs the workload. Cannot host zone-redundant
# HA — consistent with postgres_zone_redundant = false above.
postgres_sku_name = "B_Standard_B1ms"

# Smallest cache that exercises the real code path. No SLA — acceptable in UAT.
# Balanced_B0 is Azure Managed Redis's entry tier and costs materially more than
# the retired Basic/C0 it replaces; there is no cheaper AMR option.
redis_sku_name                  = "Balanced_B0"
redis_high_availability_enabled = false
# 14.43 — Entra ID is the Api's identity provider in Azure (14.47). Both values
# are IDENTIFIERS, not secrets: the authority appears in every token this API
# validates, and the audience is an app registration's client id. They arrive as
# plain env vars, never as Key Vault references — a vault read per revision start
# to conceal a value printed in every JWT would be ceremony, not security.
#
# Register the `user` and `admin` APP ROLES on this app registration, or every
# token authenticates and no token authorises (docs/auth.md). The audience is
# also what SMOKE_TOKEN_RESOURCE must be set to on the uat environment (14.47).
#
# gitleaks reads both lines as generic-api-key on shape alone. They are not:
# the first is a public OIDC issuer URL, the second an app-registration client
# id, and both already sit in src/CurrencyTracker.Api/appsettings.Azure.json.
# Allowed inline rather than repo-wide, so the next value that trips the rule
# still has to be argued.
api_authentication_authority = "https://login.microsoftonline.com/04b94fa0-2449-42e3-b19d-3275d586556a/v2.0" # gitleaks:allow
api_authentication_audience  = "e50b769e-1b9e-487d-baf5-7108f98935f2"                                        # gitleaks:allow

# 14.37 — gh-deploy-prod's SERVICE PRINCIPAL object id: the read-only promotion
# path into this environment's ACR (az acr import by digest). An object id is an
# identifier, not a secret. Populate with:
#   az ad sp list --display-name gh-deploy-prod --query "[0].id" -o tsv
# (the service principal's id, NOT the app registration's — the classic Entra
# mix-up that surfaces later as a 403 on the import).
# Left unset until then: null keeps the grant out of the plan entirely.
# promotion_pull_principal_id = "<object-id of the gh-deploy-prod service principal>"
