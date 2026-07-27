# The environment's single Log Analytics workspace. Everything queryable in
# 14.F (Container Apps console/system logs, diagnostic settings, alert rules)
# lands here. PerGB2018 is the standard pay-as-you-go SKU; 30 days is the
# retention floor and enough for an operational window — long-term archive is
# modules/storage-logs (14.25), not workspace retention.

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

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  sku               = "PerGB2018"
  retention_in_days = 30

  tags = var.tags
}
