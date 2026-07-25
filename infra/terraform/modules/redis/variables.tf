variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the cache is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the cache."
  type        = string
}

variable "sku_name" {
  description = "Azure Managed Redis SKU, e.g. Balanced_B0 (entry tier), Balanced_B1, MemoryOptimized_M10. Replaces the retired product's sku_name + family + capacity triple."
  type        = string
}

variable "high_availability_enabled" {
  description = "Provision the replica that carries the SLA (PROD). false halves the cost and drops the SLA — the UAT posture, mirroring postgres_zone_redundant."
  type        = bool
}

variable "enable_public_network_access" {
  description = "true: publicly reachable over TLS (UAT). false: private endpoint only (PROD)."
  type        = bool
}

variable "private_endpoint_subnet_id" {
  description = "Subnet hosting the private endpoint (from modules/network). Consumed only when enable_public_network_access = false."
  type        = string
}

variable "vnet_id" {
  description = "Environment VNet ID for the privatelink DNS zone link. Consumed only when enable_public_network_access = false."
  type        = string
}

variable "tags" {
  description = "Tags applied to every resource in this module."
  type        = map(string)
}
