# The Worker Container App. Deliberate absences are the design:
#  - NO ingress block: nothing calls the Worker; it has no listener at all.
#  - ONE replica: Quartz runs unclustered (Phase 12) — N replicas fire every
#    cron N times. The (RuleId, AsOfDate) idempotency index makes duplicates
#    harmless, not free. Scale-out is a future issue (clustering/queues),
#    not a knob to turn here.

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

resource "azurerm_container_app" "worker" {
  name                         = "ca-${var.name_prefix}-worker"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  identity {
    type = "SystemAssigned"
  }

  # Off until 14.D — same bootstrap cycle as the API; see that module's comment.
  dynamic "registry" {
    for_each = var.use_acr_registry ? [1] : []

    content {
      server   = var.acr_login_server
      identity = "System"
    }
  }

  dynamic "secret" {
    for_each = var.key_vault_secrets

    content {
      name                = secret.key
      key_vault_secret_id = secret.value
      identity            = "System"
    }
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "worker"
      image  = var.image
      cpu    = 0.5
      memory = "1Gi"

      dynamic "env" {
        for_each = var.env_vars

        content {
          name  = env.key
          value = env.value
        }
      }

      dynamic "env" {
        for_each = var.secret_env_vars

        content {
          name        = env.key
          secret_name = env.value
        }
      }
    }
  }

  # Same treaty as the API: 14.D's az containerapp update owns the image.
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
    ]
  }

  tags = var.tags
}
