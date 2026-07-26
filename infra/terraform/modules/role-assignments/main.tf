# The least-privilege ledger. Reading this file top to bottom answers
# "what can the workloads touch?" — keep it that readable.
#
# Two grants are deliberately NOT azurerm_role_assignment:
#  - Redis Entra access is an ACCESS-POLICY assignment (the cache's own
#    model); there is no ARM role named "Redis Data Contributor".
#  - Postgres "azure_ad_admin" is an ADMINISTRATOR REGISTRATION on the
#    server. Coarse by design of the build plan issue; in-database
#    least-privilege principals are 14.E's refinement.
#
# principal_type = "ServicePrincipal" on every RBAC grant skips the
# Entra-replication lookup that intermittently fails for just-created
# managed identities ("principal does not exist in directory").

data "azurerm_client_config" "current" {}

locals {
  app_principals = {
    api    = var.api_principal_id
    worker = var.worker_principal_id
  }
}

# --- ACR: pull images, nothing else -----------------------------------------
resource "azurerm_role_assignment" "acr_pull" {
  for_each = local.app_principals

  scope                = var.acr_id
  role_definition_name = "AcrPull"
  principal_id         = each.value
  principal_type       = "ServicePrincipal"
}

# --- Key Vault: read secret VALUES (RBAC data plane), not manage them --------
resource "azurerm_role_assignment" "kv_secrets_user" {
  for_each = local.app_principals

  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = each.value
  principal_type       = "ServicePrincipal"
}

# --- Postgres: Entra administrator registration (both apps connect via MI) ---
resource "azurerm_postgresql_flexible_server_active_directory_administrator" "app" {
  for_each = local.app_principals

  server_name         = var.postgres_server_name
  resource_group_name = var.postgres_resource_group_name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  object_id           = each.value
  principal_name      = "ca-${each.key}-identity"
  principal_type      = "ServicePrincipal"
}

# --- Redis: Data Contributor access policy — API ONLY. The Worker never
# opens a cache connection (query slice is API-side; docs/caching.md), so it
# gets no grant. Asymmetry on purpose; grants follow code.
#
# (Was azurerm_redis_cache_access_policy_assignment until the Azure Cache for
# Redis retirement forced modules/redis onto AMR.) The AMR resource takes only
# the target and the principal: no name, and no policy name — AMR exposes a
# single built-in access policy, so there is nothing to select. That also means
# no object_id_alias, so this grant has no equivalent of the principal_type
# hint above; a just-created identity may need a retry while Entra replicates.
resource "azurerm_managed_redis_access_policy_assignment" "api_data_contributor" {
  managed_redis_id = var.managed_redis_id
  object_id        = var.api_principal_id
}
# 14.37 — promotion read path: the PROD deploy identity pulls the UAT-soaked
# image by digest during `az acr import`. Pull-only, this registry only, and
# only where the caller values the principal (UAT). This is the single
# cross-environment edge in the whole design; keeping it in the Terraform
# ledger — rather than as a manual grant — is what makes it reviewable.
resource "azurerm_role_assignment" "promotion_acr_pull" {
  count = var.promotion_pull_principal_id == null ? 0 : 1

  scope                = var.acr_id
  role_definition_name = "AcrPull"
  principal_id         = var.promotion_pull_principal_id
  principal_type       = "ServicePrincipal"
}
