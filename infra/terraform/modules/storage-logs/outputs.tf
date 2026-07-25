output "account_name" {
  description = "Storage account name — 14.54's upload step targets it with --auth-mode login."
  value       = azurerm_storage_account.logs.name
}

output "account_id" {
  description = "Storage account resource ID (scope for the deploy identity's Blob Data Contributor grant when 14.54 wires uploads)."
  value       = azurerm_storage_account.logs.id
}

output "deploy_logs_container_name" {
  description = "Container receiving archived deploy logs."
  value       = azurerm_storage_container.deploy_logs.name
}