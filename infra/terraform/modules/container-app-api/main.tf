# The API Container App. Terraform owns the infrastructure shape; the deploy
# pipeline owns the image (ignore_changes below is the treaty). The system-
# assigned identity minted here is the principal every 14.24 grant targets.

resource "azurerm_container_app" "api" {
  name                         = "ca-${var.name_prefix}-api"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  identity {
    type = "SystemAssigned"
  }

  # How to authenticate to the ACR when an image lives there. Inert against
  # the public placeholder; live the moment 14.D deploys <acr>/...:sha.
  registry {
    server   = var.acr_login_server
    identity = "System"
  }

  ingress {
    external_enabled           = true
    target_port                = var.target_port
    transport                  = "http" # platform terminates TLS; container speaks HTTP
    allow_insecure_connections = false  # HTTP is refused at the platform edge

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # Key Vault-referenced secrets (empty until 14.E). The app's own system
  # identity fetches each secret — no value ever passes through Terraform.
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
      name   = "api"
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

      # Probes target the Phase 13.B health contract; gated until the real
      # image (which serves those paths) is deployed by 14.D.
      dynamic "liveness_probe" {
        for_each = var.health_probes_enabled ? [1] : []

        content {
          transport = "HTTP"
          port      = var.target_port
          path      = "/health/live"
        }
      }

      dynamic "readiness_probe" {
        for_each = var.health_probes_enabled ? [1] : []

        content {
          transport = "HTTP"
          port      = var.target_port
          path      = "/health/ready"
        }
      }
    }

    http_scale_rule {
      name                = "http-scale"
      concurrent_requests = var.http_concurrent_requests
    }
  }

  # The treaty with 14.D: az containerapp update --image deploys; Terraform
  # never reverts the image to the bootstrap placeholder.
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
    ]
  }

  tags = var.tags
}