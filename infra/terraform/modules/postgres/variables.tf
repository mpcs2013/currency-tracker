variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the server is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the server."
  type        = string
}

variable "sku_name" {
  description = "Flexible Server SKU (tier + name pattern, e.g. B_Standard_B1ms, GP_Standard_D2s_v3)."
  type        = string
}

variable "zone_redundant" {
  description = "Whether to enable ZoneRedundant high availability. Requires a GP_*/MO_* sku_name."
  type        = bool
}

variable "enable_public_network_access" {
  description = "true: public + TLS (UAT). false: VNet-injected via the delegated subnet with a private DNS zone (PROD)."
  type        = bool
}

variable "delegated_subnet_id" {
  description = "Subnet delegated to Microsoft.DBforPostgreSQL/flexibleServers (from modules/network). Consumed only when enable_public_network_access = false."
  type        = string
}

variable "vnet_id" {
  description = "Environment VNet ID for the private DNS zone link. Consumed only when enable_public_network_access = false."
  type        = string
}

variable "tags" {
  description = "Tags applied to every resource in this module."
  type        = map(string)
}