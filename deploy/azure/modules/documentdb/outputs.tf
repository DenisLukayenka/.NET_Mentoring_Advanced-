output "primary_connection_string" {
  value     = var.resource_count > 0 ? azurerm_mongo_cluster.primary.connection_strings[0].value : ""
  sensitive = true
}

output "replica_connection_string" {
  value     = var.resource_count > 0 ? azurerm_mongo_cluster.replica.connection_strings[0].value : ""
  sensitive = true
}
