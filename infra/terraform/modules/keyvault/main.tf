# Key Vault in RBAC mode. No access_policy block exists in this module, ever:
# data-plane access is granted as RBAC in modules/role-assignments (14.24).

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

data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 4
  lower   = true
  upper   = false
  special = false
}

locals {
  # Vault names are global DNS labels, 3-24 chars: e.g. kv-ct-uat-a1b2 (14).
  vault_name = "kv-${var.name_prefix}-${random_string.suffix.result}"
  private    = var.enable_public_network_access == false
}

resource "azurerm_key_vault" "this" {
  name                = local.vault_name
  resource_group_name = var.resource_group_name
  location            = var.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  # Invariants. rbac_authorization_enabled is the azurerm 4.x name
  # (3.x: enable_rbac_authorization — removed in 5.0). Purge protection is a
  # one-way door: it cannot be disabled once enabled. On before secrets exist.
  rbac_authorization_enabled = true
  purge_protection_enabled   = true
  soft_delete_retention_days = 90

  public_network_access_enabled = var.enable_public_network_access

  tags = var.tags
}

# PROD-only private path — same three-resource pattern as modules/redis.
resource "azurerm_private_dns_zone" "keyvault" {
  count = local.private ? 1 : 0

  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "keyvault" {
  count = local.private ? 1 : 0

  name                  = "pdnslink-${var.name_prefix}-keyvault"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.keyvault[0].name
  virtual_network_id    = var.vnet_id
  tags                  = var.tags
}

resource "azurerm_private_endpoint" "keyvault" {
  count = local.private ? 1 : 0

  name                = "pe-${var.name_prefix}-keyvault"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = var.private_endpoint_subnet_id

  private_service_connection {
    name                           = "psc-${var.name_prefix}-keyvault"
    private_connection_resource_id = azurerm_key_vault.this.id
    subresource_names              = ["vault"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [azurerm_private_dns_zone.keyvault[0].id]
  }

  tags = var.tags
}

# 14.42 — the values 14.43 references. content_type is a note to a human
# reading the vault, not a behaviour. Writing these needs DATA-PLANE access
# (Key Vault Secrets Officer on this vault); the deploy identity's grant is a
# one-time bootstrap recorded in docs/azure/bootstrap.md, deliberately not
# self-granted here.
resource "azurerm_key_vault_secret" "this" {
  for_each = var.secrets

  name         = each.key
  value        = each.value
  key_vault_id = azurerm_key_vault.this.id
  content_type = "application/x-connection-string"

  tags = var.tags
}
