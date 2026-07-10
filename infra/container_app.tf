# Container Apps environment + the app, scale-to-zero (min_replicas = 0). Runs in Azure mode with
# the managed identity; the only secret is the Postgres connection string.
locals {
  postgres_connection_string = "Host=${azurerm_postgresql_flexible_server.pg.fqdn};Database=${azurerm_postgresql_flexible_server_database.db.name};Username=${var.postgres_admin_login};Password=${var.postgres_admin_password};SSL Mode=Require;Trust Server Certificate=true"
}

resource "azurerm_container_app_environment" "cae" {
  name                       = local.names.container_env
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id

  tags = local.tags
}

resource "azurerm_container_app" "app" {
  name                         = local.names.container_app
  container_app_environment_id = azurerm_container_app_environment.cae.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  tags = local.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.app.id
  }

  secret {
    name  = "postgres-connection"
    value = local.postgres_connection_string
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "web"
      image  = "${azurerm_container_registry.acr.login_server}/fca-handbook-assistant:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      # Real Azure embeddings drive retrieval; the answer wording is synthesised locally because the
      # subscription has no chat-completion quota (see docs/deploy). Mode=Local selects the local
      # grounded chat client, while Embeddings:Provider=Azure uses text-embedding-3-small.
      env {
        name  = "Ai__Mode"
        value = "Local"
      }
      env {
        name  = "Ai__Embeddings__Provider"
        value = "Azure"
      }
      env {
        name  = "Ai__Embeddings__Dimensions"
        value = "1536"
      }
      env {
        name  = "Ai__RetrievalFloor"
        value = "0.35"
      }
      env {
        name  = "Ai__Azure__OpenAiEndpoint"
        value = azurerm_cognitive_account.foundry.endpoint
      }
      env {
        name  = "Ai__Azure__EmbeddingDeployment"
        value = var.embedding_deployment
      }
      env {
        name  = "Ai__Azure__ContentSafetyEndpoint"
        value = azurerm_cognitive_account.content_safety.endpoint
      }
      env {
        name        = "ConnectionStrings__Postgres"
        secret_name = "postgres-connection"
      }
      env {
        name  = "AZURE_CLIENT_ID"
        value = azurerm_user_assigned_identity.app.client_id
      }
      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = azurerm_application_insights.appi.connection_string
      }
      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }
    }
  }
}
