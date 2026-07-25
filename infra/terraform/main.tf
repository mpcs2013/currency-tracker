# Root composition. In 14.B this configures naming and reads the pre-existing
# 14.A environment resource group; 14.C composes the reusable modules below.

locals {
  # Resource group created by hand in 14.A (not Terraform-managed here).
  resource_group_name = "rg-currencytracker-${var.environment}"

  # Merge caller tags with invariants every resource should carry.
  common_tags = merge(var.tags, {
    application = "currencytracker"
    environment = var.environment
    managed_by  = "terraform"
  })
}

# Read (do not create) the environment resource group provisioned in 14.A.
# This is the foundation's one live read: it proves provider auth, backend init,
# and RG visibility on a clean `plan` without declaring any managed resource.
data "azurerm_resource_group" "env" {
  name = local.resource_group_name
}

# --- Module seam (wired one PR each in 14.C) --------------------------------
# The reusable modules compose here, each consuming data.azurerm_resource_group.env
# and local.common_tags. They are intentionally absent in 14.B: their target
# directories under ./modules do not exist yet, so declaring them now would break
# `terraform init`. 14.C adds them in dependency order, for example:
#
#   module "network"  { source = "./modules/network"  ... }   # 14.15
#   module "acr"      { source = "./modules/acr"      ... }   # 14.16
#   module "postgres" { source = "./modules/postgres" ... }   # 14.17
#   ... through storage-logs (14.25)
# ---------------------------------------------------------------------------