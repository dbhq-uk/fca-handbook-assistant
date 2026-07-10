# Resource group and shared references. All resources are named via local.names.* (CAF naming,
# see docs/design/azure-naming.md) and tagged via local.tags.
data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "main" {
  name     = local.names.resource_group
  location = var.location
  tags     = local.tags
}
