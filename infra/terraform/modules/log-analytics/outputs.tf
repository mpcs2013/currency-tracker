output "id" {
  description = "Workspace resource ID (Container Apps environment binding in 14.21; App Insights workspace_id)."
  value       = azurerm_log_analytics_workspace.this.id
}

output "name" {
  description = "Workspace name (KQL saved functions in 14.50 target it)."
  value       = azurerm_log_analytics_workspace.this.name
}