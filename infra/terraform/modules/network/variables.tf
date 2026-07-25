# Input contract for the network module. Everything environment-varying
# arrives from the root; nothing is hardcoded here.

variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the network resources are created in."
  type        = string
}

variable "location" {
  description = "Azure region for the network resources."
  type        = string
}

variable "vnet_address_space" {
  description = "Address space for the environment VNet, e.g. [\"10.20.0.0/16\"]. Must not overlap between environments."
  type        = list(string)
}

variable "tags" {
  description = "Tags applied to every resource in this module."
  type        = map(string)
}