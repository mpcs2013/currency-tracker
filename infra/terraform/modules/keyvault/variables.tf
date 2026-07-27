variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the vault is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the vault."
  type        = string
}

variable "enable_public_network_access" {
  description = "true: publicly reachable (UAT). false: private endpoint only (PROD)."
  type        = bool
}

variable "private_endpoint_subnet_id" {
  description = "Subnet hosting the private endpoint (from modules/network). Consumed only when enable_public_network_access = false."
  type        = string
}

variable "vnet_id" {
  description = "Environment VNet ID for the privatelink DNS zone link. Consumed only when enable_public_network_access = false."
  type        = string
}

variable "tags" {
  description = "Tags applied to every resource in this module."
  type        = map(string)
}

variable "secrets" {
  description = "Secrets Terraform owns end to end: name => value. Only for CREDENTIAL-FREE values (connection strings whose auth is Entra). A real password must NOT be added here — it would land in Terraform state; set it out of band with `az keyvault secret set` and reference it with a data source instead. Deliberately NOT marked sensitive: the map keys become resource instance keys, and Terraform rejects a sensitive for_each outright. That is safe only because of the credential-free rule above — and the values are redacted in plan regardless, since the provider marks azurerm_key_vault_secret.value sensitive. If a value ever needs real secrecy, it does not belong in this variable at all."
  type        = map(string)
  default     = {}
}