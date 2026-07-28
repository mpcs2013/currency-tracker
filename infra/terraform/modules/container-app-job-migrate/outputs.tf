output "id" {
  description = "Container App Job resource ID."
  value       = azurerm_container_app_job.migrate.id
}

output "name" {
  description = "Container App Job name (`az containerapp job start` in _reusable-db-migrate.yml targets it)."
  value       = azurerm_container_app_job.migrate.name
}

output "principal_id" {
  description = "Object ID of the system-assigned identity — the principal modules/role-assignments registers as a Postgres administrator and grants AcrPull and Key Vault Secrets User."
  value       = azurerm_container_app_job.migrate.identity[0].principal_id
}
