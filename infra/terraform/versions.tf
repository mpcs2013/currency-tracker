terraform {
  # Pin the Terraform core version. 1.9 is the floor for the backend and
  # provider features this project relies on; the upper bound is left open
  # so patch/minor Terraform upgrades are painless.
  required_version = ">= 1.15"

  required_providers {
    # Azure Resource Manager provider — creates every Azure resource in 14.C+.
    # ~> 4.0 accepts any 4.x (current: 4.81.0) and rejects 5.x breaking changes.
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }

    # Used by 14.C modules to generate globally-unique suffixes for resources
    # whose names must be unique across Azure (storage accounts, Key Vault).
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}