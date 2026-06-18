resource "azurerm_mongo_cluster" "primary" {
  count               = var.resource_count
  name                = "docdb-${var.project_name}-${var.env}-primary"
  resource_group_name = var.resource_group_name
  location            = var.location

  administrator_username = var.admin_username
  administrator_password = var.admin_password

  compute_tier           = var.compute_tier
  high_availability_mode = "Disabled"
  shard_count            = 1
  storage_size_in_gb     = var.storage_size_in_gb
  version                = "8.0"
}

resource "azurerm_mongo_cluster" "replica" {
  count               = var.resource_count
  name                = "docdb-${var.project_name}-${var.env}-replica"
  resource_group_name = var.resource_group_name
  location            = var.secondary_location

  create_mode      = "GeoReplica"
  source_server_id = azurerm_mongo_cluster.primary[count.index].id
  source_location  = var.location

  depends_on = [azurerm_mongo_cluster.primary]
}

resource "azurerm_mongo_cluster_firewall_rule" "allow_azure" {
  count            = var.resource_count
  name             = "AllowAzureServices"
  mongo_cluster_id = azurerm_mongo_cluster.primary[count.index].id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"

  depends_on = [azurerm_mongo_cluster.primary]
}

resource "azurerm_mongo_cluster_firewall_rule" "custom" {
  for_each         = var.resource_count > 0 ? { for r in var.allowed_ip_ranges : r.name => r } : {}
  name             = each.value.name
  mongo_cluster_id = azurerm_mongo_cluster.primary[0].id
  start_ip_address = each.value.start_ip
  end_ip_address   = each.value.end_ip

  depends_on = [azurerm_mongo_cluster.primary]
}

resource "azurerm_mongo_cluster_firewall_rule" "allow_azure_replica" {
  count            = var.resource_count
  name             = "AllowAzureServices"
  mongo_cluster_id = azurerm_mongo_cluster.replica[count.index].id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"

  depends_on = [azurerm_mongo_cluster.replica]
}

resource "azurerm_mongo_cluster_firewall_rule" "custom_replica" {
  for_each         = var.resource_count > 0 ? { for r in var.allowed_ip_ranges : r.name => r } : {}
  name             = each.value.name
  mongo_cluster_id = azurerm_mongo_cluster.replica[0].id
  start_ip_address = each.value.start_ip
  end_ip_address   = each.value.end_ip

  depends_on = [azurerm_mongo_cluster.replica]
}
