terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.1"
    }
  }

  # Remote state in the shared azure-housekeeping backend. The state account is IP-locked;
  # run terraform from an allow-listed machine. CI validates with -backend=false.
  backend "azurerm" {
    resource_group_name  = "rg-dbhq-housekeeping"
    storage_account_name = "dbhqtfstateuks01"
    container_name       = "tfstate"
    key                  = "fca-handbook-assistant.tfstate"
  }
}
