output "endpoint" {
  value       = var.resource_count > 0 ? azurerm_cosmosdb_account.jobs[0].endpoint : ""
  description = "Cosmos DB NoSQL account endpoint."
}

output "primary_key" {
  value       = var.resource_count > 0 ? azurerm_cosmosdb_account.jobs[0].primary_key : ""
  sensitive   = true
  description = "Cosmos DB NoSQL account primary key."
}

output "primary_connection_string" {
  value       = var.resource_count > 0 ? azurerm_cosmosdb_account.jobs[0].connection_strings[0] : ""
  sensitive   = true
  description = "Cosmos DB NoSQL account primary connection string."
}
