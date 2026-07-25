output "id" {
  description = "Container App resource ID."
  value       = azurerm_container_app.worker.id
}

output "name" {
  description = "Container App name (az containerapp update in 14.33 targets it)."
  value       = azurerm_container_app.worker.name
}

output "principal_id" {
  description = "Object ID of the system-assigned identity — the principal 14.24 grants."
  value       = azurerm_container_app.worker.identity[0].principal_id
}