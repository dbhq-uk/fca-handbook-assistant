# Azure AI Search (free tier) - the cloud parity variant of the pgvector store ("evidence both").
resource "azurerm_search_service" "search" {
  name                = local.names.ai_search
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "free"

  tags = local.tags
}
