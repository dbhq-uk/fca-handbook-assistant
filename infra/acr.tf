# Azure Container Registry for the app image (built with `az acr build`), pulled by the Container
# App via its managed identity (AcrPull) - no registry credentials in the app.
resource "azurerm_container_registry" "acr" {
  name                = local.names.container_registry
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = false

  tags = local.tags
}

resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
}
