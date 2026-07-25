variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the vault is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the vault."
  type        = string
}

variable "enable_public_network_access" {
  description = "true: publicly reachable (UAT). false: private endpoint only (PROD)."
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