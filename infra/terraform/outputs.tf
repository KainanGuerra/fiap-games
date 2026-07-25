output "resource_group_name" {
  description = "Name of the resource group holding every resource."
  value       = azurerm_resource_group.main.name
}

output "container_app_fqdn" {
  description = "Public HTTPS hostname of the API container app."
  value       = azurerm_container_app.api.latest_revision_fqdn
}

output "cosmos_db_account_name" {
  description = "Name of the Cosmos DB (Mongo API) account."
  value       = azurerm_cosmosdb_account.main.name
}

output "cosmos_db_connection_string" {
  description = "Primary Mongo connection string for the Cosmos DB account."
  value       = azurerm_cosmosdb_account.main.primary_mongodb_connection_string
  sensitive   = true
}

output "container_app_environment_id" {
  description = "ID of the Container Apps environment, useful for adding further apps later."
  value       = azurerm_container_app_environment.main.id
}
