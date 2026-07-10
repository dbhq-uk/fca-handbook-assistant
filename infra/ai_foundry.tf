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

# Note: a Foundry project (azurerm_cognitive_account_project) requires the account to be created
# with allowProjectManagement = true, which azurerm_cognitive_account (v4.80) does not expose. The
# RAG pipeline uses the account's Azure OpenAI endpoint directly and does not need a project, so the
# project is omitted; the server-managed Foundry agent path is out of scope for this deploy.

# No chat deployment: this Sponsorship subscription has zero real-time chat-completion quota
# (GA chat models are GlobalStandard-only at 0 quota; the Standard-SKU models - gpt-4o 2024-11-20,
# gpt-4.1-mini 2025-04-14 - are past their new-deployment cutoff). The deployed app therefore uses
# real Azure embeddings for retrieval and a deterministic local synthesiser for the answer wording;
# the code targets an Azure chat deployment unchanged when chat quota is available. See
# docs/deploy/2026-07-10-azure-deploy.md.

resource "azurerm_cognitive_deployment" "embedding" {
  name                 = var.embedding_deployment
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  model {
    format  = "OpenAI"
    name    = "text-embedding-3-small"
    version = "1"
  }

  # text-embedding-3-small is only offered on GlobalStandard (Sponsorship quota: 1000).
  sku {
    name     = "GlobalStandard"
    capacity = 50
  }
}
