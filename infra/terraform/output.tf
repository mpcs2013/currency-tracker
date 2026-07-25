# Foundation outputs. Consumed by 14.C module wiring and useful for verifying
# that `plan` really reached Azure and resolved the environment resource group.

output "resource_group_name" {
  description = "Name of the environment resource group (read from 14.A)."
  value       = data.azurerm_resource_group.env.name
}

output "resource_group_location" {
  description = "Azure region of the environment resource group."
  value       = data.azurerm_resource_group.env.location
}

output "name_prefix" {
  description = "Resource-name prefix for this environment."
  value       = var.name_prefix
}

output "app_insights_connection_string" {
  description = "Application Insights connection string for this environment (14.48/14.E consume it via terraform output -raw)."
  value       = module.app_insights.connection_string
  sensitive   = true
}