resource "azurerm_cosmosdb_account" "job_outputs" {
  count               = var.resource_count
  name                = "cosmos-cassandra-${var.project_name}-${var.env}"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  automatic_failover_enabled = true

  consistency_policy {
    consistency_level = "Eventual"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }

  geo_location {
    location          = var.secondary_location
    failover_priority = 1
  }

  capabilities {
    name = "EnableCassandra"
  }
}

resource "azurerm_cosmosdb_cassandra_keyspace" "scheduler" {
  count               = var.resource_count
  name                = "scheduler"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.job_outputs[count.index].name

  autoscale_settings {
    max_throughput = var.max_throughput
  }
}

resource "azurerm_cosmosdb_cassandra_table" "job_outputs" {
  count                 = var.resource_count
  name                  = "job_outputs"
  cassandra_keyspace_id = azurerm_cosmosdb_cassandra_keyspace.scheduler[count.index].id

  default_ttl_seconds = var.job_outputs_ttl_seconds

  schema {
    column {
      name = "job_id"
      type = "uuid"
    }
    column {
      name = "date"
      type = "timestamp"
    }
    column {
      name = "id"
      type = "uuid"
    }
    column {
      name = "level"
      type = "text"
    }
    column {
      name = "message"
      type = "text"
    }

    partition_key {
      name = "job_id"
    }

    cluster_key {
      name     = "date"
      order_by = "Asc"
    }
  }
}
