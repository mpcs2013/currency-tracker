variable "name_prefix" {
  description = "Resource-name prefix for this environment (e.g. ct-uat)."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the server is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the server."
  type        = string
}

variable "sku_name" {
  description = "Flexible Server SKU (tier + name pattern, e.g. B_Standard_B1ms, GP_Standard_D2s_v3)."
  type        = string
}

variable "zone_redundant" {
  description = "Whether to enable ZoneRedundant high availability. Requires a GP_*/MO_* sku_name."
  type        = bool
}

variable "enable_public_network_access" {
  description = "true: public + TLS (UAT). false: VNet-injected via the delegated subnet with a private DNS zone (PROD)."
  type        = bool
}

variable "allow_azure_services" {
  description = "Create the 0.0.0.0 firewall rule that admits traffic from Azure resources. Required in the public posture: a Flexible Server with publicNetworkAccess=Enabled and NO firewall rules denies EVERY connection, which is what stalled the first UAT deploy — the Api's readiness probe hung on a dropped TCP connect to 5432. Container Apps cannot be allow-listed by address (an external environment's outbound pool is ~160 IPs and is not stable), so this is the only workable grain. It is a network rule, not an authentication one: password auth is off, so reaching the port still gets you nothing without an Entra token for a registered administrator. Ignored when enable_public_network_access = false — the private posture has no firewall to configure."
  type        = bool
  default     = false
}

variable "allowed_ip_ranges" {
  description = "Named IPv4 ranges admitted in addition to Azure services, e.g. an office range for psql access. Keyed by rule name so a range can be removed without renumbering the rest. Ignored in the private posture."
  type = map(object({
    start_ip = string
    end_ip   = string
  }))
  default = {}
}

variable "delegated_subnet_id" {
  description = "Subnet delegated to Microsoft.DBforPostgreSQL/flexibleServers (from modules/network). Consumed only when enable_public_network_access = false."
  type        = string
}

variable "vnet_id" {
  description = "Environment VNet ID for the private DNS zone link. Consumed only when enable_public_network_access = false."
  type        = string
}

variable "tags" {
  description = "Tags applied to every resource in this module."
  type        = map(string)
}