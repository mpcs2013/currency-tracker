# Provider configuration for the root module.

provider "azurerm" {
  # The features {} block is mandatory for azurerm (even when empty); it gates
  # provider-level behaviours for certain resource types, tuned per module later.
  features {}

  # azurerm 4.x requires a subscription id. It is supplied via ARM_SUBSCRIPTION_ID
  # (locally from `az account show`; in CI from azure/login over OIDC) so the GUID
  # never lands in the repo. No client secret: auth is az login / OIDC federation.
}

provider "random" {
  # No configuration; consumed by 14.C modules for globally-unique name suffixes.
}