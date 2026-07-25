output "id" {
  description = "Managed Redis resource ID (the 14.24 access-policy assignment targets it)."
  value       = azurerm_managed_redis.this.id
}

output "name" {
  description = "Cache name."
  value       = azurerm_managed_redis.this.name
}

output "hostname" {
  description = "Cache hostname — the host the 14.E connection configuration points at."
  value       = azurerm_managed_redis.this.hostname
}

output "port" {
  description = "TLS port for client connections. Read from the resource rather than assumed: AMR does not use the 6380 the retired product did, and 14.E must not inherit the old value."
  value       = azurerm_managed_redis.this.default_database[0].port
}
