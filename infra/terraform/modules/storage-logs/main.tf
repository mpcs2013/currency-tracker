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
  #checkov:skip=CKV_AZURE_59:Network access is open by necessity, not omission — see public_network_access_enabled below. The credential surface is closed instead: Entra-only, no shared key, no anonymous read.
  #checkov:skip=CKV2_AZURE_33:No private endpoint, for the same reason: the writers are GitHub-hosted runners outside the VNet. A private endpoint here would need self-hosted runners first.
  #checkov:skip=CKV_AZURE_206:LRS is deliberate — see the comment on account_replication_type. These are archives of logs reproducible from the pipeline that wrote them.
  #checkov:skip=CKV2_AZURE_1:No customer-managed key. CMK buys key custody, which matters for data this account does not hold: deploy logs, no personal or financial data. Platform-managed encryption stays on regardless.
  #checkov:skip=CKV_AZURE_33:Storage Analytics logging for the QUEUE service, which this account does not enable and nothing uses. Logging a service that does not exist is not a control.
  name                = local.account_name
  resource_group_name = var.resource_group_name
  location            = var.location

  account_tier             = "Standard"
  account_replication_type = "LRS" # archives of reproducible logs; LRS suffices

  # Hardening invariants.
  shared_access_key_enabled       = false    # Entra-only; no key/SAS path exists
  min_tls_version                 = "TLS1_2" # azurerm 4.x default, pinned explicitly
  allow_nested_items_to_be_public = false    # no anonymous blob/container ever

  # Explicit, not defaulted — checkov flagged this as an open network and it was
  # genuinely undecided until now. It stays TRUE in BOTH environments, unlike
  # the ACR/Key Vault/Postgres trio that follow enable_public_network_access:
  # every writer is a GitHub-hosted runner (14.54) whose egress IP is dynamic
  # and outside the VNet. Closing the network without first moving those uploads
  # to a self-hosted runner or a private endpoint would break PROD deploy
  # logging, which is exactly when logs matter most.
  #
  # What carries the security here is the credential surface above, not the
  # perimeter: no shared key, no SAS, no anonymous access — reaching this
  # account is not the same as reading it.
  public_network_access_enabled = true

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 7
    }
  }

  tags = var.tags
}

resource "azurerm_storage_container" "deploy_logs" {
  #checkov:skip=CKV2_AZURE_21:Blob read-logging on the log archive itself. The reads are a human opening a deploy log; recording them here would grow the archive faster than the archive grows, and the audit trail that matters (who wrote what) is the pipeline run record.
  name                  = "deploy-logs"
  storage_account_id    = azurerm_storage_account.logs.id
  container_access_type = "private"
}
