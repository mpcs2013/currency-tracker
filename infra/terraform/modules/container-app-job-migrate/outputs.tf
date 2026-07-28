output "id" {
  description = "Container App Job resource ID."
  value       = azurerm_container_app_job.migrate.id
}

output "name" {
  description = "Container App Job name (`az containerapp job start` in _reusable-db-migrate.yml targets it)."
  value       = azurerm_container_app_job.migrate.name
}

# No principal_id output. The identity is user-assigned and declared in the root
# module, which is where modules/role-assignments reads it from — exporting it
# back out of here would reintroduce the dependency cycle this design removed.
