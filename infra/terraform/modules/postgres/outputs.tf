output "id" {
  description = "Server resource ID."
  value       = azurerm_postgresql_flexible_server.this.id
}

output "name" {
  description = "Server name (the Entra-administrator registration in 14.24 targets it)."
  value       = azurerm_postgresql_flexible_server.this.name
}

output "fqdn" {
  description = "Server FQDN — the host the 14.E connection configuration points at."
  value       = azurerm_postgresql_flexible_server.this.fqdn
}

output "database_name" {
  description = "Application database name — the Database= segment of the 14.42 connection string."
  value       = azurerm_postgresql_flexible_server_database.app.name
}