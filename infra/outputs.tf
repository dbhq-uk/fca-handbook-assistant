output "app_url" {
  description = "Public URL of the Container App."
  value       = "https://${azurerm_container_app.app.ingress[0].fqdn}"
}

output "foundry_endpoint" {
  description = "Azure AI Foundry (AIServices) endpoint used by the RAG pipeline."
  value       = azurerm_cognitive_account.foundry.endpoint
}

output "search_endpoint" {
  description = "Azure AI Search endpoint (cloud parity variant)."
  value       = "https://${azurerm_search_service.search.name}.search.windows.net"
}

output "postgres_fqdn" {
  description = "Postgres Flexible Server FQDN."
  value       = azurerm_postgresql_flexible_server.pg.fqdn
}

output "app_identity_client_id" {
  description = "Client id of the app's managed identity."
  value       = azurerm_user_assigned_identity.app.client_id
}
