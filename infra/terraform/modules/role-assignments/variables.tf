variable "api_principal_id" {
  description = "Object ID of the API app's system-assigned identity."
  type        = string
}

variable "worker_principal_id" {
  description = "Object ID of the Worker app's system-assigned identity."
  type        = string
}

variable "acr_id" {
  description = "Registry resource ID — AcrPull scope."
  type        = string
}

variable "key_vault_id" {
  description = "Key Vault resource ID — Key Vault Secrets User scope."
  type        = string
}

variable "redis_cache_id" {
  description = "Redis cache resource ID — Data Contributor access-policy target."
  type        = string
}

variable "postgres_server_name" {
  description = "Postgres Flexible Server name — Entra administrator registration target."
  type        = string
}

variable "postgres_resource_group_name" {
  description = "Resource group of the Postgres server."
  type        = string
}