# One hardened registry per environment. ACR names are GLOBAL DNS labels,
# alphanumeric only (no hyphens) — hence the stripped prefix + random suffix.

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

    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

resource "random_string" "suffix" {
  length  = 4
  lower   = true
  upper   = false
  special = false
}

locals {
  # "ct-uat" -> "ctuat"; final name e.g. "crctuata1b2".
  registry_name = "cr${replace(var.name_prefix, "-", "")}${random_string.suffix.result}"

  # Premium where the environment is private-endpoint-bound (PROD): 14.56
  # attaches the endpoint, and private endpoints require the Premium SKU.
  # Standard suffices for UAT's public + firewall posture.
  sku = var.enable_public_network_access ? "Standard" : "Premium"
}

resource "azurerm_container_registry" "this" {
  name                = local.registry_name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = local.sku

  # Hardening invariants — not variables on purpose. Every consumer
  # authenticates as itself: MI + AcrPull (14.24) or pipeline OIDC (14.31).
  admin_enabled          = false
  anonymous_pull_enabled = false

  # Public access remains enabled in BOTH envs for now: PROD's private
  # endpoint is issue 14.56, and flipping this to false before the endpoint
  # exists would strand every push and pull. 14.56 turns it off for PROD.
  public_network_access_enabled = true

  tags = var.tags
}
