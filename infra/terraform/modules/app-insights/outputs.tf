output "id" {
  description = "Component resource ID."
  value       = azurerm_application_insights.this.id
}

output "connection_string" {
  description = "Ingestion connection string — the value 14.48/14.E hand to the apps. Redacted in output by default."
  value       = azurerm_application_insights.this.connection_string
  sensitive   = true
}