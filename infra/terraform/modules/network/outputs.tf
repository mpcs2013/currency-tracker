# Output contract. Later modules reference these through the root:
# module.network.<output>. Nothing else in this module is visible to callers.

output "vnet_id" {
  description = "ID of the environment VNet (private DNS zone links attach here)."
  value       = azurerm_virtual_network.this.id
}

output "vnet_name" {
  description = "Name of the environment VNet."
  value       = azurerm_virtual_network.this.name
}

output "infrastructure_subnet_id" {
  description = "Delegated subnet for the Container Apps environment (14.21)."
  value       = azurerm_subnet.infrastructure.id
}

output "private_endpoint_subnet_id" {
  description = "Subnet hosting private endpoints (14.18/14.19/14.56)."
  value       = azurerm_subnet.private_endpoints.id
}

output "postgres_subnet_id" {
  description = "Delegated subnet for VNet-injected Postgres (14.17, PROD)."
  value       = azurerm_subnet.postgres.id
}