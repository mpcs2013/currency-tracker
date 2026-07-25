# UAT environment values. Cost-leaning: single-zone, public access + firewall.
# Contains NO secrets — secrets resolve from Key Vault (Phase 14.E).
environment = "uat"
location    = "switzerlandnorth"
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