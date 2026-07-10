# Azure AI Foundry (AIServices account) + project, with chat and embedding model deployments.
# The account exposes an Azure OpenAI-compatible endpoint used by the grounded RAG pipeline; the
# project is the target for the Microsoft Agent Framework Foundry agent.
resource "azurerm_cognitive_account" "foundry" {
  name                  = local.names.foundry_account
  location              = azurerm_resource_group.main.location
  resource_group_name   = azurerm_resource_group.main.name
  kind                  = "AIServices"
  sku_name              = "S0"
  custom_subdomain_name = local.names.foundry_account

  identity {
    type = "SystemAssigned"
  }

  tags = local.tags
}

resource "azurerm_cognitive_account_project" "project" {
  name                 = local.names.foundry_project
  location             = azurerm_resource_group.main.location
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_cognitive_deployment" "chat" {
  name                 = var.chat_deployment
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  model {
    format  = "OpenAI"
    name    = "gpt-4o-mini"
    version = "2024-07-18"
  }

  sku {
    name     = "GlobalStandard"
    capacity = 20
  }
}

resource "azurerm_cognitive_deployment" "embedding" {
  name                 = var.embedding_deployment
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  model {
    format  = "OpenAI"
    name    = "text-embedding-3-small"
    version = "1"
  }

  sku {
    name     = "Standard"
    capacity = 50
  }
}
