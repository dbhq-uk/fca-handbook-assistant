# Log Analytics + Application Insights - token cost, latency, refusal rate, and citation telemetry
# flow here from the app via OpenTelemetry.
resource "azurerm_log_analytics_workspace" "logs" {
  name                = local.names.log_analytics
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.tags
}

resource "azurerm_application_insights" "appi" {
  name                = local.names.app_insights
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.logs.id

  tags = local.tags
}
