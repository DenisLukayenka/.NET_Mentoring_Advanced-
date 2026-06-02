resource "azurerm_cosmosdb_account" "jobs" {
  count               = var.resource_count
  name                = "cosmos-nosql-${var.project_name}-${var.env}"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  free_tier_enabled          = true
  automatic_failover_enabled = true

  consistency_policy {
    consistency_level = "ConsistentPrefix"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }

  geo_location {
    location          = var.secondary_location
    failover_priority = 1
  }
}

resource "azurerm_cosmosdb_sql_database" "scheduler" {
  count               = var.resource_count
  name                = "scheduler"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.jobs[count.index].name

  autoscale_settings {
    max_throughput = var.max_throughput
  }
}

resource "azurerm_cosmosdb_sql_container" "jobs" {
  count               = var.resource_count
  name                = "jobs"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.jobs[count.index].name
  database_name       = azurerm_cosmosdb_sql_database.scheduler[count.index].name

  partition_key_paths   = ["/JobDefinitionId"]
  partition_key_version = 2

  unique_key {
    paths = ["/JobDefinitionId", "/ScheduledAt"]
  }

  indexing_policy {
    indexing_mode = "consistent"

    included_path {
      path = "/*"
    }

    excluded_path {
      path = "/\"_etag\"/?"
    }
  }
}

import {
  to = module.cosmosdb_nosql.azurerm_cosmosdb_account.jobs[0]
  id = "/subscriptions/dbc052ff-21d7-478e-a0fe-d73ef7665c5b/resourceGroups/rg-scheduler-dev-eastus/providers/Microsoft.DocumentDB/databaseAccounts/cosmos-nosql-scheduler-dev"
}
