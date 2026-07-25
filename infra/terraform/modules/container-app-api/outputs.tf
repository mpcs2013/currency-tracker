output "id" {
  description = "Container App resource ID."
  value       = azurerm_container_app.api.id
}

output "name" {
  description = "Container App name (az containerapp update in 14.33 targets it)."
  value       = azurerm_container_app.api.name
}

output "principal_id" {
  description = "Object ID of the system-assigned identity — the principal 14.24 grants."
  value       = azurerm_container_app.api.identity[0].principal_id
}

output "fqdn" {
  description = "Ingress FQDN of the app's latest revision."
  value       = azurerm_container_app.api.latest_revision_fqdn
}