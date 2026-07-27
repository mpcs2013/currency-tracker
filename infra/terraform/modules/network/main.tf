# One VNet per environment, three purpose-built subnets. Subnet prefixes are
# DERIVED from the single vnet_address_space input so they cannot drift or
# overlap: /23 for Container Apps infrastructure (slots 0-1 of the /24 grid),
# /24 for private endpoints (slot 2), /24 for VNet-injected Postgres (slot 3).

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

locals {
  infrastructure_subnet_prefix   = cidrsubnet(var.vnet_address_space[0], 7, 0)
  private_endpoint_subnet_prefix = cidrsubnet(var.vnet_address_space[0], 8, 2)
  postgres_subnet_prefix         = cidrsubnet(var.vnet_address_space[0], 8, 3)
}

resource "azurerm_virtual_network" "this" {
  name                = "vnet-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  address_space       = var.vnet_address_space
  tags                = var.tags
}

# Infrastructure subnet for the Container Apps environment (14.21).
# Delegation to Microsoft.App/environments is REQUIRED for the workload-
# profiles architecture; sizing /23 leaves headroom for revision doubling
# during zero-downtime deploys. Both properties force-new the environment
# if changed later — decided here, once.
resource "azurerm_subnet" "infrastructure" {
  #checkov:skip=CKV2_AZURE_31:No NSG yet. Container Apps requires a specific set of allow rules on its infrastructure subnet, and an NSG missing one of them breaks the environment in ways that surface as unexplained revision failures. Worth doing deliberately, not as a side effect.
  name                 = "snet-${var.name_prefix}-aca"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = [local.infrastructure_subnet_prefix]

  delegation {
    name = "aca-delegation"

    service_delegation {
      name    = "Microsoft.App/environments"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
}

# Home for private endpoints (redis 14.18, keyvault 14.19, ACR in 14.56).
# private_endpoint_network_policies defaults to Disabled in azurerm 4.x,
# which is what private endpoints need — left implicit on purpose.
resource "azurerm_subnet" "private_endpoints" {
  #checkov:skip=CKV2_AZURE_31:No NSG yet. Private endpoints ignore NSG rules unless private_endpoint_network_policies is Enabled, which azurerm 4.x defaults to Disabled (see the comment above) — an NSG here would read as a control while enforcing nothing.
  name                 = "snet-${var.name_prefix}-pe"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = [local.private_endpoint_subnet_prefix]
}

# Exclusive delegated subnet for VNet-injected Postgres Flexible Server
# (14.17, PROD). Flexible Server requires the delegation and tolerates no
# other resource in the subnet. Created in both envs (address space is free);
# consumed only where enable_public_network_access = false.
resource "azurerm_subnet" "postgres" {
  #checkov:skip=CKV2_AZURE_31:No NSG yet. The subnet is delegated exclusively to Flexible Server and holds nothing else, so its blast radius is one server that already refuses password auth; an NSG is defence in depth worth adding with the other two, together.
  name                 = "snet-${var.name_prefix}-postgres"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.this.name
  address_prefixes     = [local.postgres_subnet_prefix]

  delegation {
    name = "postgres-delegation"

    service_delegation {
      name    = "Microsoft.DBforPostgreSQL/flexibleServers"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
}
