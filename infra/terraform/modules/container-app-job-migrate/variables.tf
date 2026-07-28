variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the job is created in."
  type        = string
}

variable "location" {
  description = "Azure region. Required on azurerm_container_app_job, unlike azurerm_container_app which infers it from the environment."
  type        = string
}

variable "container_app_environment_id" {
  description = "Container Apps environment hosting the job (from modules/container-apps-env). Placing the job here is what gives it PROD's VNet integration and private DNS resolution for Postgres — the reason a CI runner cannot do this work."
  type        = string
}

variable "acr_login_server" {
  description = "Registry login server the job pulls from via its system identity."
  type        = string
}

variable "use_acr_registry" {
  description = "Declare the ACR registry credential on the job. Must stay false until the AcrPull grant exists for THIS job's identity — the same first-apply bootstrap cycle the two app modules document. Flip it in the same change that points `image` at the ACR."
  type        = bool
  default     = false
}

variable "image" {
  description = "Bootstrap image only; the deploy pipeline owns the image after first apply (ignore_changes in main.tf). The placeholder contains no migrations, which is why the pipeline must always pass an explicit digest rather than relying on whatever the job last ran."
  type        = string
  default     = "mcr.microsoft.com/dotnet/samples:aspnetapp"
}

variable "replica_timeout_in_seconds" {
  description = "Ceiling on one migration run. Default 1800 (30 min): long enough for an index build against a cold database, short enough that a hung execution fails the deploy instead of parking it. Raise only with a migration that demonstrably needs it."
  type        = number
  default     = 1800
}

variable "env_vars" {
  description = "Plain environment variables (name => value). Must carry the same ASPNETCORE_ENVIRONMENT and Azure__UseManagedIdentity the Worker gets, or the job resolves a different configuration than the app it is migrating for."
  type        = map(string)
  default     = {}
}

variable "secret_env_vars" {
  description = "Secret-backed environment variables (name => container-app secret name). Carries ConnectionStrings__currencytracker; the job needs no cache connection because it never opens one."
  type        = map(string)
  default     = {}
}

variable "key_vault_secrets" {
  description = "Container-app secrets resolved from Key Vault via the system identity: name => Key Vault secret ID. Leave empty on a fresh environment's first apply — the identity is minted by this resource and its Key Vault Secrets User grant does not exist yet, so the reference would fail to resolve. Same two-pass bootstrap as the app modules."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags applied to the job."
  type        = map(string)
}
