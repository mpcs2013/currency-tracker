variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the app is created in."
  type        = string
}

variable "container_app_environment_id" {
  description = "Container Apps environment hosting the app (from modules/container-apps-env)."
  type        = string
}

variable "acr_login_server" {
  description = "Registry login server the app pulls from via its system identity once 14.D deploys real images."
  type        = string
}

variable "image" {
  description = "Bootstrap image only; the deploy pipeline owns the image after first apply (ignore_changes)."
  type        = string
  default     = "mcr.microsoft.com/dotnet/samples:aspnetapp"
}

variable "min_replicas" {
  description = "Minimum replicas. Default 1 — see the Quartz rationale in main.tf before changing."
  type        = number
  default     = 1
}

variable "max_replicas" {
  description = "Maximum replicas. Default 1: unclustered Quartz means N replicas fire every cron N times."
  type        = number
  default     = 1
}

variable "env_vars" {
  description = "Plain environment variables (name => value). 14.E extends."
  type        = map(string)
  default     = {}
}

variable "secret_env_vars" {
  description = "Secret-backed environment variables (name => container-app secret name). Populated in 14.E."
  type        = map(string)
  default     = {}
}

variable "key_vault_secrets" {
  description = "Container-app secrets resolved from Key Vault via the system identity: name => Key Vault secret ID. Populated in 14.E."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags applied to the app."
  type        = map(string)
}