# Container Apps environment. Three arguments here are force-new
# (infrastructure_subnet_id, internal_load_balancer_enabled,
# zone_redundancy_enabled) — this module is a bundle of permanent decisions.

# Provider requirements for this directory. The root module pins the same
# constraints in versions.tf; these are the floor a caller must satisfy, and
# what tflint reads when --recursive lints this module as a standalone root.
terraform {
  required_version = ">= 1.15"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

resource "azurerm_container_app_environment" "this" {
  name                = "cae-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  # The delegated /23 from 14.15. Supplying a delegated subnet selects the
  # workload-profiles architecture; the profile below is the one apps pin to.
  infrastructure_subnet_id = var.infrastructure_subnet_id

  # PROD: ingress fronted by an internal load balancer inside the VNet —
  # the "no public network access" posture made real. UAT: external.
  internal_load_balancer_enabled = var.enable_public_network_access == false

  # Console + system logs stream into the environment's workspace (14.20).
  logs_destination           = "log-analytics"
  log_analytics_workspace_id = var.log_analytics_workspace_id

  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }

  # zone_redundancy_enabled stays at its default (false) deliberately: it is
  # force-new, and enabling it is an environment-rebuild decision. If PROD
  # wants it, the flip must land BEFORE 14.D's first PROD apply — its own
  # issue, decided with eyes open, not a default flipped here in passing.

  tags = var.tags
}
