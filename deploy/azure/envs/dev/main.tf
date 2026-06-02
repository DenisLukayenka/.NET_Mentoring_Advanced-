locals {
  common_tags = {
    created_by  = "denis.lukayenka@gmail.com"
    environment = var.env
  }
}

terraform {
  backend "remote" {
    organization = "LearnHub"

    workspaces {
      name = "azure-scheduler-dev"
    }
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  count    = var.resource_count
  name     = "rg-${var.project_name}-${var.env}-${var.location}"
  location = var.location
  tags     = local.common_tags
}

module "documentdb" {
  source              = "../../modules/documentdb"
  resource_count      = var.resource_count
  location            = var.location
  secondary_location  = var.secondary_location
  project_name        = var.project_name
  env                 = var.env
  resource_group_name = try(azurerm_resource_group.rg[0].name, "")
  admin_password      = var.documentdb_admin_password

  allowed_ip_ranges = [
    {
      name     = "local-dev"
      start_ip = var.local_dev_ip
      end_ip   = var.local_dev_ip
    }
  ]

  depends_on = [azurerm_resource_group.rg]
}

module "cosmosdb_nosql" {
  source              = "../../modules/cosmosdb-nosql"
  resource_count      = var.resource_count
  location            = var.location
  secondary_location  = var.secondary_location
  project_name        = var.project_name
  env                 = var.env
  resource_group_name = try(azurerm_resource_group.rg[0].name, "")

  depends_on = [azurerm_resource_group.rg]
}

module "cosmosdb_cassandra" {
  source              = "../../modules/cosmosdb-cassandra"
  resource_count      = var.resource_count
  location            = var.location
  secondary_location  = var.secondary_location
  project_name        = var.project_name
  env                 = var.env
  resource_group_name = try(azurerm_resource_group.rg[0].name, "")

  depends_on = [azurerm_resource_group.rg]
}
