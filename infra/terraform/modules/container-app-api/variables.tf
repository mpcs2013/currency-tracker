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

variable "use_acr_registry" {
  description = "Declare the ACR registry credential on the app. Must stay false until 14.24's AcrPull grant exists for this app's identity — see the bootstrap-cycle note in main.tf. Flip it in the same change that points `image` at the ACR."
  type        = bool
  default     = false
}

variable "image" {
  description = "Bootstrap image only. Public placeholder listening on 8080; after first apply the deploy pipeline owns the image (ignore_changes)."
  type        = string
  default     = "mcr.microsoft.com/dotnet/samples:aspnetapp"
}

variable "target_port" {
  description = "Container port ingress forwards to. 8080 = ASP.NET Core default; the API's Dockerfile (14.26) exposes the same."
  type        = number
  default     = 8080
}

variable "min_replicas" {
  description = "Minimum replicas. 1 keeps the API warm; 0 would scale to zero between requests."
  type        = number
  default     = 1
}

variable "max_replicas" {
  description = "Maximum replicas for the HTTP scale rule."
  type        = number
  default     = 3
}

variable "http_concurrent_requests" {
  description = "Concurrent requests per replica that trigger scale-out."
  type        = number
  default     = 50
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

variable "health_probes_enabled" {
  description = "Wire liveness/readiness probes to /health/live + /health/ready. Off until the real image (which serves them) ships in 14.D."
  type        = bool
  default     = false
}

variable "tags" {
  description = "Tags applied to the app."
  type        = map(string)
}