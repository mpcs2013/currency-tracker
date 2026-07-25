# UAT remote-state coordinates. Points at the 14.A state backend. No secrets:
# these are resource identifiers, and access is via the caller's AAD identity.
resource_group_name  = "rg-currencytracker-tfstate"
storage_account_name = "stcurrencytrackertfstate"
container_name       = "tfstate"
key                  = "uat.terraform.tfstate"