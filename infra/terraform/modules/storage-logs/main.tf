# Long-term log/artifact archive. Entra-only from birth: every writer (deploy
# workflows over OIDC in 14.54, humans via az login) is an Entra principal,
# so the shared-key credential class is disabled outright.

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
  # Storage names: global, 3-24 chars, lowercase alphanumeric only.
  # "ct-uat" -> "stctuatlogsa1b2" (15 chars).
  account_name = "st${replace(var.name_prefix, "-", "")}logs${random_string.suffix.result}"
}

resource "azurerm_storage_account" "logs" {
  name                = local.account_name
  resource_group_name = var.resource_group_name
  location            = var.location

  account_tier             = "Standard"
  account_replication_type = "LRS" # archives of reproducible logs; LRS suffices

  # Hardening invariants.
  shared_access_key_enabled       = false    # Entra-only; no key/SAS path exists
  min_tls_version                 = "TLS1_2" # azurerm 4.x default, pinned explicitly
  allow_nested_items_to_be_public = false    # no anonymous blob/container ever

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 7
    }
  }

  tags = var.tags
}

resource "azurerm_storage_container" "deploy_logs" {
  name                  = "deploy-logs"
  storage_account_id    = azurerm_storage_account.logs.id
  container_access_type = "private"
}
