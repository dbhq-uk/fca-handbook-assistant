# Azure resources for the assistant: Azure OpenAI + model deployments, AI Search,
# Container Apps (scale-to-zero), Postgres (pgvector), Key Vault, App Insights,
# Content Safety. Remote state lives in the azure-housekeeping backend.
#
# First real task: add the resources (or wrap AVM resource modules) and a
# backend "azurerm" block pointing at the shared state container.
