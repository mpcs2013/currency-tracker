# Postgres Flexible Server, Entra-only. No administrator_login/_password
# exists in this file, in tfvars, or in state — password auth is OFF, and
# access is exactly the Entra principals registered as administrators (14.24).

data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 4
  lower   = true
  upper   = false
  special = false
}

locals {
  # Server names are global DNS labels: <name>.postgres.database.azure.com.
  server_name = "psql-${var.name_prefix}-${random_string.suffix.result}"
  private     = var.enable_public_network_access == false
}

# PROD-only: private DNS zone (suffix is service-mandated) + link into the
# environment VNet, so the Container Apps resolve the server's private IP.
resource "azurerm_private_dns_zone" "postgres" {
  count = local.private ? 1 : 0

  name                = "${replace(var.name_prefix, "-", "")}.postgres.database.azure.com"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "postgres" {
  count = local.private ? 1 : 0

  name                  = "pdnslink-${var.name_prefix}-postgres"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.postgres[0].name
  virtual_network_id    = var.vnet_id
  tags                  = var.tags
}

resource "azurerm_postgresql_flexible_server" "this" {
  name                = local.server_name
  resource_group_name = var.resource_group_name
  location            = var.location

  # Version 18 — parity with ADR 0009's pinned dev/test image (postgres:18).
  version    = "18"
  sku_name   = var.sku_name
  storage_mb = 32768

  # Entra-only. password_auth_enabled = false is what makes administrator_
  # login/_password unnecessary — and therefore absent from state.
  authentication {
    active_directory_auth_enabled = true
    password_auth_enabled         = false
    tenant_id                     = data.azurerm_client_config.current.tenant_id
  }

  # PROD: injected into the delegated subnet with private DNS; the provider
  # requires public access off when these are set. UAT: both null, public on.
  public_network_access_enabled = var.enable_public_network_access
  delegated_subnet_id           = local.private ? var.delegated_subnet_id : null
  private_dns_zone_id           = local.private ? azurerm_private_dns_zone.postgres[0].id : null

  dynamic "high_availability" {
    for_each = var.zone_redundant ? [1] : []

    content {
      mode = "ZoneRedundant"
    }
  }

  # After a failover Azure updates zone/standby zone in place; without this,
  # the next plan would try to migrate the server back — a self-inflicted
  # failover. Documented provider guidance, adopted verbatim.
  lifecycle {
    ignore_changes = [
      zone,
      high_availability[0].standby_availability_zone,
    ]
  }

  tags = var.tags

  depends_on = [azurerm_private_dns_zone_virtual_network_link.postgres]
}