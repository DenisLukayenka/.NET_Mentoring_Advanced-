output "primary_connection_string" {
  value     = var.resource_count > 0 ? azurerm_mongo_cluster.primary[0].connectionString : ""
  sensitive = true
}

output "replica_connection_string" {
  value     = var.resource_count > 0 ? azurerm_mongo_cluster.replica[0].connectionString : ""
  sensitive = true
}
