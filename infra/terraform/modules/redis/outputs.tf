output "id" {
  description = "Cache resource ID (the 14.24 access-policy assignment targets it)."
  value       = azurerm_redis_cache.this.id
}

output "name" {
  description = "Cache name."
  value       = azurerm_redis_cache.this.name
}

output "hostname" {
  description = "Cache hostname — the host the 14.E connection configuration points at (TLS port 6380)."
  value       = azurerm_redis_cache.this.hostname
}