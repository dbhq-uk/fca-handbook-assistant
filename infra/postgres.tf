# Burstable Postgres Flexible Server with pgvector - the app's retrieval store and audit log.
# Stop it when idle between sessions to avoid cost.
resource "azurerm_postgresql_flexible_server" "pg" {
  name                          = local.names.postgres
  location                      = azurerm_resource_group.main.location
  resource_group_name           = azurerm_resource_group.main.name
  version                       = "16"
  administrator_login           = var.postgres_admin_login
  administrator_password        = var.postgres_admin_password
  sku_name                      = "B_Standard_B1ms"
  storage_mb                    = 32768
  zone                          = "1"
  public_network_access_enabled = true

  tags = local.tags
}

# Allow-list pgvector, then the app runs CREATE EXTENSION vector.
resource "azurerm_postgresql_flexible_server_configuration" "vector" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.pg.id
  value     = "VECTOR"
}

resource "azurerm_postgresql_flexible_server_database" "db" {
  name      = "fca_handbook"
  server_id = azurerm_postgresql_flexible_server.pg.id
}

# The Container App (an Azure service) reaches Postgres via the Azure-internal rule (0.0.0.0).
resource "azurerm_postgresql_flexible_server_firewall_rule" "azure_services" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.pg.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Optionally allow a workstation IP so ingestion can be run from outside Azure.
resource "azurerm_postgresql_flexible_server_firewall_rule" "client" {
  count            = var.allowed_client_ip != "" ? 1 : 0
  name             = "allow-client"
  server_id        = azurerm_postgresql_flexible_server.pg.id
  start_ip_address = var.allowed_client_ip
  end_ip_address   = var.allowed_client_ip
}
