variable "api_principal_id" {
  description = "Object ID of the API app's system-assigned identity."
  type        = string
}

variable "worker_principal_id" {
  description = "Object ID of the Worker app's system-assigned identity."
  type        = string
}

variable "migrate_principal_id" {
  description = "Object ID of the schema-migration job's USER-assigned identity (14.48, ADR 0018), read from the standalone identity resource in the root module rather than from the job. That is the whole point: these grants must exist before the job provisions, and a job's system-assigned identity would not exist until it did. Receives AcrPull, Key Vault Secrets User and a Postgres administrator registration — but no Redis grant, because it opens no cache connection."
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

variable "managed_redis_id" {
  description = "Managed Redis resource ID — access-policy assignment target."
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
variable "promotion_pull_principal_id" {
  description = "Object ID of the PROD deploy identity (gh-deploy-prod) that may PULL from this environment's ACR for `az acr import` promotion. null everywhere except the UAT envelope — the deliberate, read-only crack in the environment wall (14.37)."
  type        = string
  default     = null
}
