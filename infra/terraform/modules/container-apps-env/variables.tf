variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the environment is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the environment."
  type        = string
}

variable "infrastructure_subnet_id" {
  description = "Subnet delegated to Microsoft.App/environments (from modules/network). Force-new."
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "Workspace receiving the environment's console/system logs (from modules/log-analytics)."
  type        = string
}

variable "enable_public_network_access" {
  description = "true: external environment (UAT). false: internal-load-balancer mode (PROD). Force-new."
  type        = bool
}

variable "tags" {
  description = "Tags applied to the environment."
  type        = map(string)
}