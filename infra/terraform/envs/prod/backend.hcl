# PROD remote-state coordinates. Same container as UAT; a DIFFERENT key is the
# entire cross-environment state isolation. No secrets; AAD-authenticated.
resource_group_name  = "rg-currencytracker-tfstate"
storage_account_name = "stcurrencytrackertfstate"
container_name       = "tfstate"
key                  = "prod.terraform.tfstate"