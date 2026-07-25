output "id" {
  description = "Registry resource ID (AcrPull scope in 14.24; import source in 14.D)."
  value       = azurerm_container_registry.this.id
}

output "login_server" {
  description = "Registry login server, e.g. crctuata1b2.azurecr.io — image references and the apps' registry blocks use this."
  value       = azurerm_container_registry.this.login_server
}

output "name" {
  description = "Registry name (az acr commands in 14.D take the bare name)."
  value       = azurerm_container_registry.this.name
}