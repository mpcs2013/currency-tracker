output "id" {
  description = "Vault resource ID — the Key Vault Secrets User scope in 14.24."
  value       = azurerm_key_vault.this.id
}

output "name" {
  description = "Vault name."
  value       = azurerm_key_vault.this.name
}

output "vault_uri" {
  description = "Vault URI — the base of every @Microsoft.KeyVault(SecretUri=...) reference wired in 14.E."
  value       = azurerm_key_vault.this.vault_uri
}