variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the component is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the component."
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "Workspace this component stores its telemetry in (workspace-based mode)."
  type        = string
}

variable "tags" {
  description = "Tags applied to the component."
  type        = map(string)
}