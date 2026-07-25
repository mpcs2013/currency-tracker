variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the workspace is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the workspace."
  type        = string
}

variable "tags" {
  description = "Tags applied to the workspace."
  type        = map(string)
}