output "id" {
  description = "Environment resource ID — the container apps (14.22/14.23) deploy into it."
  value       = azurerm_container_app_environment.this.id
}

output "default_domain" {
  description = "Default domain of the environment (the API's FQDN lives under it; 14.38's smoke test resolves it)."
  value       = azurerm_container_app_environment.this.default_domain
}

output "static_ip_address" {
  description = "Environment ingress IP (the internal LB address in PROD — future private DNS for the API points here)."
  value       = azurerm_container_app_environment.this.static_ip_address
}