# The schema-migration job (ADR 0018). Terraform owns the job's shape; the
# deploy pipeline owns the image — the same treaty the two container apps are
# under, extended to a third resource rather than re-invented for it.
#
# Why a job at all: neither environment can be migrated from a CI runner. PROD
# Postgres has no public endpoint (enable_public_network_access = false, VNet
# injection + private DNS), and the deploy identities are not registered
# database administrators. Running inside the environment solves both at once.
#
# Deliberate choices, each one load-bearing:
#  - MANUAL trigger only. Nothing schedules this. The deploy workflow starts
#    exactly one execution, before the new revision goes live.
#  - parallelism = 1, replica_completion_count = 1. One replica applies the
#    schema. Concurrent migration is the failure this forbids outright.
#  - replica_retry_limit = 0. A half-applied migration must not be retried into
#    a second partial application; a human reads the logs instead.
#  - Its OWN system-assigned identity, and therefore a THIRD Postgres
#    administrator registration in modules/role-assignments. A Container Apps
#    job cannot borrow another resource's system-assigned identity, so "reuse
#    the Worker's" was never available — see that module's ledger entry.
#  - args = ["--migrate"], appended to the image ENTRYPOINT. The runtime image
#    is chiseled and has no shell, so an args override is the only mechanism
#    available; there is no `sh -c` to fall back on.

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

resource "azurerm_container_app_job" "migrate" {
  name                         = "caj-${var.name_prefix}-migrate"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  container_app_environment_id = var.container_app_environment_id
  workload_profile_name        = "Consumption"

  # The ceiling on a single migration run. Generous enough for an index build
  # on a cold database, short enough that a hung execution fails the deploy
  # rather than parking it. Postgres DDL is transactional, so a replica killed
  # at this boundary rolls back rather than leaving a half-applied schema.
  replica_timeout_in_seconds = var.replica_timeout_in_seconds
  replica_retry_limit        = 0

  identity {
    type = "SystemAssigned"
  }

  manual_trigger_config {
    parallelism              = 1
    replica_completion_count = 1
  }

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
    container {
      name   = "migrate"
      image  = var.image
      cpu    = 0.5
      memory = "1Gi"

      # Selects migrate-and-exit in the Worker host. The match is exact and
      # ordinal on the application side: a typo here does NOT start a normal
      # Worker, it is rejected by the JasperFx command line and the execution
      # fails — which is the whole point of spelling it out in one place.
      args = ["--migrate"]

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

  # The image treaty (ADR 0015, extended by ADR 0018). `az containerapp job
  # start --image <digest>` is the only thing that moves this. Widening or
  # removing this to make a plan clean re-points the job at the bootstrap
  # placeholder, which would then "migrate" using an image containing no
  # migrations.
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
    ]
  }

  tags = var.tags
}
