# PROD environment values. Resilience + perimeter: zone-redundant, private.
# Contains NO secrets — secrets resolve from Key Vault (Phase 14.E).
environment = "prod"
location    = "switzerlandnorth"
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