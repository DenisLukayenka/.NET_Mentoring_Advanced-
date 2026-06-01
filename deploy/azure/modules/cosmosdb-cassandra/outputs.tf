output "endpoint" {
  value       = var.resource_count > 0 ? azurerm_cosmosdb_account.job_outputs[0].endpoint : ""
  description = "Cosmos DB Cassandra account endpoint."
}

output "primary_key" {
  value       = var.resource_count > 0 ? azurerm_cosmosdb_account.job_outputs[0].primary_key : ""
  sensitive   = true
  description = "Cosmos DB Cassandra account primary key."
}
