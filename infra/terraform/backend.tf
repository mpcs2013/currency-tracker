terraform {
  # Partial backend configuration. The environment-specific coordinates
  # (resource group, storage account, container, state key) are supplied at
  # init time from envs/<env>/backend.hcl via -backend-config. The one setting
  # that never varies lives here: authenticate to the state blob with the
  # caller's Entra (AAD) identity — never a storage account key (ADR 0014).
  backend "azurerm" {
    use_azuread_auth = true
  }
}