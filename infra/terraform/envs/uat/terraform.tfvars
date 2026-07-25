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