# Root composition. In 14.B this configures naming and reads the pre-existing
# 14.A environment resource group; 14.C composes the reusable modules below.

locals {
  # Resource group created by hand in 14.A (not Terraform-managed here).
  resource_group_name = "rg-currencytracker-${var.environment}"

  # Merge caller tags with invariants every resource should carry.
  common_tags = merge(var.tags, {
    application = "currencytracker"
    environment = var.environment
    managed_by  = "terraform"
  })
}

# Read (do not create) the environment resource group provisioned in 14.A.
# This is the foundation's one live read: it proves provider auth, backend init,
# and RG visibility on a clean `plan` without declaring any managed resource.
data "azurerm_resource_group" "env" {
  name = local.resource_group_name
}

# --- Module seam (wired one PR each in 14.C) --------------------------------
# The reusable modules compose here, each consuming data.azurerm_resource_group.env
# and local.common_tags. They are intentionally absent in 14.B: their target
# directories under ./modules do not exist yet, so declaring them now would break
# `terraform init`. 14.C adds them in dependency order, for example:
#
# 14.15 — VNet + subnets consumed by postgres (14.17), redis/keyvault private
# endpoints (14.18/14.19), and the Container Apps environment (14.21).
module "network" {
  source = "./modules/network"

  name_prefix         = var.name_prefix
  resource_group_name = data.azurerm_resource_group.env.name
  location            = data.azurerm_resource_group.env.location
  vnet_address_space  = var.vnet_address_space
  tags                = local.common_tags
}
# 14.16 — one registry per environment (see the walkthrough for why not
# shared): pushed by main-ci (14.35), pulled by the apps' MIs (14.24),
# imported into PROD by digest during deploy-prod (14.D).
module "acr" {
  source = "./modules/acr"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  location                     = data.azurerm_resource_group.env.location
  enable_public_network_access = var.enable_public_network_access
  tags                         = local.common_tags
}
# 14.17 — system of record. Entra-only auth (no password exists anywhere);
# VNet-injected + private DNS in PROD, public + TLS in UAT.
module "postgres" {
  source = "./modules/postgres"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  location                     = data.azurerm_resource_group.env.location
  sku_name                     = var.postgres_sku_name
  zone_redundant               = var.postgres_zone_redundant
  enable_public_network_access = var.enable_public_network_access
  delegated_subnet_id          = module.network.postgres_subnet_id
  vnet_id                      = module.network.vnet_id
  tags                         = local.common_tags
}
# 14.18 — distributed cache (Phase 10 query slice; /health/ready dependency).
# Entra auth on; private endpoint materialises only in the private posture.
module "redis" {
  source = "./modules/redis"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  location                     = data.azurerm_resource_group.env.location
  sku_name                     = var.redis_sku_name
  capacity                     = var.redis_capacity
  enable_public_network_access = var.enable_public_network_access
  private_endpoint_subnet_id   = module.network.private_endpoint_subnet_id
  vnet_id                      = module.network.vnet_id
  tags                         = local.common_tags
}


#   ... through storage-logs (14.25)
# ---------------------------------------------------------------------------