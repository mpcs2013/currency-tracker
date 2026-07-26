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
  high_availability_enabled    = var.redis_high_availability_enabled
  enable_public_network_access = var.enable_public_network_access
  private_endpoint_subnet_id   = module.network.private_endpoint_subnet_id
  vnet_id                      = module.network.vnet_id
  tags                         = local.common_tags
}

# 14.19 — the secrets store every @Microsoft.KeyVault(...) reference (14.E)
# resolves from. RBAC mode: data-plane access is a 14.24 role assignment.
module "keyvault" {
  source = "./modules/keyvault"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  location                     = data.azurerm_resource_group.env.location
  enable_public_network_access = var.enable_public_network_access
  private_endpoint_subnet_id   = module.network.private_endpoint_subnet_id
  vnet_id                      = module.network.vnet_id
  tags                         = local.common_tags
}

# 14.20 — telemetry sinks. The workspace is shared plumbing (14.21 logs into
# it; 14.50-14.53 query it); App Insights is the 14.48 OTLP target.
module "log_analytics" {
  source = "./modules/log-analytics"

  name_prefix         = var.name_prefix
  resource_group_name = data.azurerm_resource_group.env.name
  location            = data.azurerm_resource_group.env.location
  tags                = local.common_tags
}

module "app_insights" {
  source = "./modules/app-insights"

  name_prefix                = var.name_prefix
  resource_group_name        = data.azurerm_resource_group.env.name
  location                   = data.azurerm_resource_group.env.location
  log_analytics_workspace_id = module.log_analytics.id
  tags                       = local.common_tags
}

# 14.21 — the hosting environment for both apps. VNet-integrated in both
# envs (parity); external LB in UAT, internal in PROD.
module "container_apps_env" {
  source = "./modules/container-apps-env"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  location                     = data.azurerm_resource_group.env.location
  infrastructure_subnet_id     = module.network.infrastructure_subnet_id
  log_analytics_workspace_id   = module.log_analytics.id
  enable_public_network_access = var.enable_public_network_access
  tags                         = local.common_tags
}

# 14.22 — the API. Placeholder image until 14.D deploys the real one;
# the MI minted here is what 14.24 grants.
module "container_app_api" {
  source = "./modules/container-app-api"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  container_app_environment_id = module.container_apps_env.id
  acr_login_server             = module.acr.login_server

  # 14.35: the deploy pipeline now ships private images from this ACR.
  # Safe to declare because 14.24's AcrPull grant has long since
  # propagated (the bootstrap cycle the module comment warns about is
  # only a first-apply problem). Applied to UAT by deploy-uat's first
  # run; to PROD by deploy-prod's first apply.
  use_acr_registry = true

  env_vars = {
    ASPNETCORE_ENVIRONMENT = var.environment == "prod" ? "Production" : "Staging"
  }

  tags = local.common_tags
}

# 14.23 — the Worker: Quartz-scheduled ingestion + outbox relay. No ingress,
# exactly one replica (unclustered Quartz — see the module comment).
module "container_app_worker" {
  source = "./modules/container-app-worker"

  name_prefix                  = var.name_prefix
  resource_group_name          = data.azurerm_resource_group.env.name
  container_app_environment_id = module.container_apps_env.id
  acr_login_server             = module.acr.login_server

  # 14.35: the deploy pipeline now ships private images from this ACR.
  # Safe to declare because 14.24's AcrPull grant has long since
  # propagated (the bootstrap cycle the module comment warns about is
  # only a first-apply problem). Applied to UAT by deploy-uat's first
  # run; to PROD by deploy-prod's first apply.
  use_acr_registry = true

  env_vars = {
    DOTNET_ENVIRONMENT = var.environment == "prod" ? "Production" : "Staging"
  }

  tags = local.common_tags
}

# 14.24 — the least-privilege ledger: everything the two workload identities
# may touch, one grant per line, scoped to single resources.
module "role_assignments" {
  source = "./modules/role-assignments"

  api_principal_id    = module.container_app_api.principal_id
  worker_principal_id = module.container_app_worker.principal_id

  acr_id                       = module.acr.id
  key_vault_id                 = module.keyvault.id
  managed_redis_id             = module.redis.id
  postgres_server_name         = module.postgres.name
  postgres_resource_group_name = data.azurerm_resource_group.env.name
}

# 14.25 — long-term archive for deploy logs/artifacts (14.54 uploads here).
# Environment-scoped lifecycle; deliberately NOT the tfstate account.
module "storage_logs" {
  source = "./modules/storage-logs"

  name_prefix         = var.name_prefix
  resource_group_name = data.azurerm_resource_group.env.name
  location            = data.azurerm_resource_group.env.location
  tags                = local.common_tags
}

#   ... through storage-logs (14.25)
# ---------------------------------------------------------------------------