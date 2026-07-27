# Root input surface. Values arrive per environment from
# envs/<env>/terraform.tfvars via -var-file. 14.C extends this per module.

variable "environment" {
  description = "Deployment environment discriminator. Drives naming and the state key."
  type        = string

  validation {
    condition     = contains(["uat", "prod"], var.environment)
    error_message = "environment must be one of: uat, prod."
  }
}

# No `location` variable: every module call in main.tf takes its region from
# `data.azurerm_resource_group.env.location`. The resource group is created by
# the manual bootstrap (docs/azure/bootstrap.md), so its region is authoritative
# and a second declaration here could only ever disagree with it.

variable "name_prefix" {
  description = "Short prefix for resource names, e.g. \"ct\" -> ca-ct-api."
  type        = string
}

variable "tags" {
  description = "Tags applied to every resource for cost and ownership tracking."
  type        = map(string)
  default     = {}
}

variable "postgres_zone_redundant" {
  description = "Whether Postgres Flexible Server is zone-redundant (true in PROD)."
  type        = bool
}

variable "enable_public_network_access" {
  description = "Whether env resources allow public network access (UAT: true + firewall; PROD: false)."
  type        = bool
}

variable "postgres_allow_azure_services" {
  description = "Admit Azure-internal traffic to Postgres (the 0.0.0.0 sentinel rule). Must be true wherever the apps reach the server over its public endpoint — a public server with no firewall rules refuses everything, silently. Pairs with enable_public_network_access: true in UAT, false in PROD where the delegated subnet carries the traffic instead."
  type        = bool
}

variable "postgres_allowed_ip_ranges" {
  description = "Named IPv4 ranges admitted to Postgres in addition to Azure services (e.g. an office range for psql). Empty by default: every range here is a hole in the perimeter that outlives whoever opened it."
  type = map(object({
    start_ip = string
    end_ip   = string
  }))
  default = {}
}

variable "vnet_address_space" {
  description = "Address space for the environment VNet. UAT and PROD must not overlap (peering-safe)."
  type        = list(string)
}

variable "postgres_sku_name" {
  description = "Flexible Server SKU. Must be an HA-capable tier (GP_*/MO_*) wherever postgres_zone_redundant = true."
  type        = string
}

variable "redis_sku_name" {
  description = "Azure Managed Redis SKU, e.g. Balanced_B0 (entry tier) or Balanced_B1. The old Basic/Standard/Premium + capacity pair died with Azure Cache for Redis."
  type        = string
}

variable "redis_high_availability_enabled" {
  description = "Whether the cache runs a replica for its SLA (true in PROD). Mirrors postgres_zone_redundant: PROD buys resilience, UAT buys the lower bill."
  type        = bool
}
variable "api_authentication_authority" {
  description = "OIDC issuer the Api validates tokens against, e.g. https://login.microsoftonline.com/<tenant-id>/v2.0. An identifier, not a secret — same posture as the 14.A AZURE_* GitHub variables, and it appears in every token this API validates anyway. Arrives at the container as Authentication__Authority. 14.47's boot-time guard asserts it agrees with the declared provider, so a stale value here kills the revision instead of 401ing every request."
  type        = string
}

variable "api_authentication_audience" {
  description = "Audience the Api requires in a token — the API app registration's client id (or api://<client-id>). An identifier, not a secret. Arrives as Authentication__Audience, and is the same value SMOKE_TOKEN_RESOURCE carries on the uat GitHub environment (14.47)."
  type        = string
}

variable "promotion_pull_principal_id" {
  description = "Object ID of gh-deploy-prod, granted AcrPull on this env's ACR for release promotion (az acr import by digest). Set in the UAT envelope only; null in PROD. See modules/role-assignments and docs/ci-cd/pipelines.md."
  type        = string
  default     = null
}
