# Azure Cache for Redis, hardened per the build plan's secure defaults.
# The three hardening arguments are invariants, not variables.

resource "random_string" "suffix" {
  length  = 4
  lower   = true
  upper   = false
  special = false
}

locals {
  # Cache names are global DNS labels: <name>.redis.cache.windows.net.
  cache_name = "redis-${var.name_prefix}-${random_string.suffix.result}"
  private    = var.enable_public_network_access == false
}

resource "azurerm_redis_cache" "this" {
  name                = local.cache_name
  resource_group_name = var.resource_group_name
  location            = var.location

  sku_name = var.sku_name
  family   = "C" # Basic/Standard family; Premium (P) has no issue in this phase
  capacity = var.capacity

  # Hardening invariants: plaintext port stays closed, TLS 1.2 floor,
  # Entra tokens accepted (14.24 assigns the access policy; 14.E connects).
  non_ssl_port_enabled = false
  minimum_tls_version  = "1.2"

  redis_configuration {
    active_directory_authentication_enabled = true
    # access_keys_authentication_enabled stays at its default (true) until
    # 14.E moves the app onto Entra tokens; 14.G closes the key path.
  }

  public_network_access_enabled = var.enable_public_network_access

  tags = var.tags
}

# PROD-only private path: endpoint in the shared PE subnet + the service's
# privatelink zone linked into the environment VNet.
resource "azurerm_private_dns_zone" "redis" {
  count = local.private ? 1 : 0

  name                = "privatelink.redis.cache.windows.net"
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
    private_connection_resource_id = azurerm_redis_cache.this.id
    subresource_names              = ["redisCache"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [azurerm_private_dns_zone.redis[0].id]
  }

  tags = var.tags
}