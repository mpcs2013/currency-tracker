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
  # Seven of these need the Premium SKU, which local.sku only selects for the
  # private (PROD) envelope — they are unreachable in UAT by construction. PROD
  # does get Premium, so enabling them there is a real hardening step that has
  # not been taken yet; it is deliberately not smuggled in under a lint fix.
  #checkov:skip=CKV_AZURE_163:Defender image scanning is a subscription-level plan, not a registry argument, and is unavailable on UAT's Standard SKU. PROD hardening, not yet scheduled.
  #checkov:skip=CKV_AZURE_164:Content trust is Premium-only; UAT runs Standard. Image provenance today is the Trivy gate in main-ci plus digest-pinned promotion (ADR 0015).
  #checkov:skip=CKV_AZURE_166:Quarantine mode is Premium-only and would stall every push until a scanner released it; main-ci's Trivy gate already blocks unscanned bytes before they reach the registry.
  #checkov:skip=CKV_AZURE_167:Untagged-manifest retention is Premium-only. Every image this repo pushes is SHA-tagged, so untagged manifests only appear on overwrite, which nothing here does.
  #checkov:skip=CKV_AZURE_237:Dedicated data endpoints are Premium-only and only matter behind a firewall; revisit with the private endpoint in 14.56.
  #checkov:skip=CKV_AZURE_165:Geo-replication is Premium-only and prices per replica region. Both environments are single-region (switzerlandnorth) by design.
  #checkov:skip=CKV_AZURE_233:Zone redundancy is Premium-only and force-new. A registry outage blocks deploys, not the running app, so it is not on the availability path this project pays for.
  #checkov:skip=CKV_AZURE_139:Public access is deliberately on in BOTH envs until 14.56 attaches the private endpoint — see the comment on public_network_access_enabled below. Turning it off first strands every push and pull.
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
