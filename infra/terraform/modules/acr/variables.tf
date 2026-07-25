variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the registry is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the registry."
  type        = string
}

variable "enable_public_network_access" {
  description = "Environment network posture. Drives the SKU: Premium where private endpoints are coming (false), Standard otherwise."
  type        = bool
}

variable "tags" {
  description = "Tags applied to the registry."
  type        = map(string)
}