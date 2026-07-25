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

variable "location" {
  description = "Azure region for environment resources (e.g. westeurope)."
  type        = string
}

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

variable "vnet_address_space" {
  description = "Address space for the environment VNet. UAT and PROD must not overlap (peering-safe)."
  type        = list(string)
}

variable "postgres_sku_name" {
  description = "Flexible Server SKU. Must be an HA-capable tier (GP_*/MO_*) wherever postgres_zone_redundant = true."
  type        = string
}