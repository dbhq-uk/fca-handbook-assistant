# Content Safety - inspects user input and model output.
resource "azurerm_cognitive_account" "content_safety" {
  name                  = local.names.content_safety
  location              = azurerm_resource_group.main.location
  resource_group_name   = azurerm_resource_group.main.name
  kind                  = "ContentSafety"
  sku_name              = "S0"
  custom_subdomain_name = local.names.content_safety

  tags = local.tags
}
