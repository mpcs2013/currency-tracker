# Workspace-based Application Insights — the target 14.48 points the
# "CurrencyTracker" OTLP pipeline at via the Azure Monitor exporter.
# workspace_id is what makes it workspace-based; classic (workspace-less)
# components are retired and would strand 14.F's single-workspace queries.

resource "azurerm_application_insights" "this" {
  name                = "appi-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  application_type = "web"
  workspace_id     = var.log_analytics_workspace_id

  tags = var.tags
}