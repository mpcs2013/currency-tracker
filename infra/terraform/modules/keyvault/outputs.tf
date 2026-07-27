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

output "secret_versionless_ids" {
  description = "name => VERSIONLESS secret id. Versionless on purpose: a Container App referencing an unversioned URI picks up a rotated value within 30 minutes and restarts the revisions that consume it, so rotation is not a redeploy."
  value       = { for name, secret in azurerm_key_vault_secret.this : name => secret.versionless_id }
}