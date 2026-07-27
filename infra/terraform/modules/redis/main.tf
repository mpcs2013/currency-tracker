# Azure Managed Redis, hardened per the build plan's secure defaults. The
# hardening arguments are invariants, not variables.
#
# This module was Azure Cache for Redis (azurerm_redis_cache) until that product
# entered retirement: the RedisCreate API now rejects every new instance with
# "Azure Cache for Redis is retiring, create Azure Managed Redis instance
# instead" (aka.ms/AzureCacheForRedisRetirement). Not a migration we chose.
#
# AMR is ONE resource: the database is a nested default_database block, not a
# separate resource, and AMR permits exactly one of them.

# Provider requirements for this directory. The root module pins the same
# constraints in versions.tf; these are the floor a caller must satisfy, and
# what tflint reads when --recursive lints this module as a standalone root.
terraform {
  required_version = ">= 1.15"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }

    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

resource "random_string" "suffix" {
  length  = 4
  lower   = true
  upper   = false
  special = false
}

locals {
  # Cache names are global DNS labels: <name>.<region>.redis.azure.net.
  cache_name = "redis-${var.name_prefix}-${random_string.suffix.result}"
  private    = var.enable_public_network_access == false
}

resource "azurerm_managed_redis" "this" {
  name                = local.cache_name
  resource_group_name = var.resource_group_name
  location            = var.location

  # One SKU string replaces the old sku_name + family + capacity triple.
  # Balanced_B0 is the entry tier — AMR has no equivalent of Basic/C0.
  sku_name = var.sku_name

  # PROD buys the replica + SLA; UAT deliberately does not (cost).
  high_availability_enabled = var.high_availability_enabled

  # A string on AMR, not the bool the retired product took.
  public_network_access = var.enable_public_network_access ? "Enabled" : "Disabled"

  default_database {
    # Hardening invariant: plaintext is refused outright. Encrypted replaces
    # non_ssl_port_enabled = false + minimum_tls_version, which AMR folds into
    # this single argument.
    client_protocol = "Encrypted"

    # EnterpriseCluster exposes the single-endpoint, non-clustered Redis API
    # that StackExchange.Redis speaks by default. OSSCluster would require a
    # cluster-aware client — a Phase 10 code change, not a Terraform knob.
    clustering_policy = "EnterpriseCluster"

    # Matches the volatile-lru default the retired Basic tier ran under, so the
    # Phase 10 query-slice cache keeps its existing eviction behaviour.
    eviction_policy = "VolatileLRU"

    # Entra-only from birth. AMR inverts the retired product's default: access
    # keys are OFF unless asked for, so the key path 14.G was going to close is
    # already shut and 14.E has no interim key-auth step to lean on. Pinned
    # explicitly rather than left to a default that differs per product.
    # Entra auth itself needs no flag — it is the 14.24 access-policy assignment.
    access_keys_authentication_enabled = false
  }

  tags = var.tags
}

# PROD-only private path: endpoint in the shared PE subnet + the service's
# privatelink zone linked into the environment VNet.
#
# UNVERIFIED — the zone name and subresource below follow the redisEnterprise
# ARM type, but no PROD apply has exercised them and the retirement moved this
# service's private-link naming. Confirm both against the AMR private-link docs
# before the first PROD apply; UAT (public posture) never instantiates them.
resource "azurerm_private_dns_zone" "redis" {
  count = local.private ? 1 : 0

  name                = "privatelink.redis.azure.net"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "redis" {
  count = local.private ? 1 : 0

  name                  = "pdnslink-${var.name_prefix}-redis"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.redis[0].name
  virtual_network_id    = var.vnet_id
  tags                  = var.tags
}

resource "azurerm_private_endpoint" "redis" {
  count = local.private ? 1 : 0

  name                = "pe-${var.name_prefix}-redis"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = var.private_endpoint_subnet_id

  private_service_connection {
    name                           = "psc-${var.name_prefix}-redis"
    private_connection_resource_id = azurerm_managed_redis.this.id
    subresource_names              = ["redisEnterprise"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [azurerm_private_dns_zone.redis[0].id]
  }

  tags = var.tags
}
