# PROD environment values. Resilience + perimeter: zone-redundant, private.
# Contains NO secrets — secrets resolve from Key Vault (Phase 14.E).
environment = "prod"
name_prefix = "ct-prod"

tags = {
  app        = "currencytracker"
  env        = "prod"
  owner      = "marco"
  costcentre = "currency-tracker"
}

# PROD is zone-redundant for Postgres high availability.
postgres_zone_redundant = true

# PROD has no public network access — private endpoints only (enforced in 14.G).
enable_public_network_access = false

# Environment VNet. Non-overlapping with UAT (10.20.0.0/16).
vnet_address_space = ["10.30.0.0/16"]

# General Purpose: the floor for zone-redundant HA (postgres_zone_redundant
# = true above). Burstable + HA is an apply-time error.
postgres_sku_name = "GP_Standard_D2s_v3"

# REVIEW BEFORE THE FIRST PROD APPLY. Standard/C1 died with Azure Cache for
# Redis; this is a like-for-like guess at its replacement, not a costed choice.
# high_availability_enabled is what buys the replica + SLA that "Standard" used
# to imply — the SKU string no longer encodes it.
redis_sku_name                  = "Balanced_B1"
redis_high_availability_enabled = true

# 14.43 — Entra ID as the Api's identity provider. Identifiers, not secrets;
# see the UAT envelope for the full reasoning.
#
# REVIEW BEFORE THE FIRST PROD APPLY, like the Redis SKU above. These are the
# SAME tenant and the SAME app registration UAT uses, because only one API app
# registration exists today (docs/azure/bootstrap.md §App Registrations lists
# the deploy identities, not this one). Sharing an audience across environments
# means a UAT-issued token is accepted by PROD. That is tolerable while PROD has
# never been applied and holds no data; it is not a steady state. When a
# prod-only registration exists, this file is the only thing that changes — the
# split costs two lines here and nothing in code, which is the whole point of
# ADR 0010's config-only provider swap.
api_authentication_authority = "https://login.microsoftonline.com/04b94fa0-2449-42e3-b19d-3275d586556a/v2.0" # gitleaks:allow
api_authentication_audience  = "e50b769e-1b9e-487d-baf5-7108f98935f2"                                        # gitleaks:allow