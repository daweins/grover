resource "random_password" "DatabasePassword" {
  length = 16
  special = true
  override_special = "_%@"
}

resource "azurerm_sql_server" "sqlserver" {
  name                         = format("%ssqlserver",var.deployment_name)
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.serverLoginName
  administrator_login_password = random_password.DatabasePassword.result
}

resource "azurerm_sql_database" "devsqldatabase" {
  name                = format("%sdatabase-dev",var.deployment_name)
  resource_group_name = var.resource_group_name
  location            = var.location
  server_name         = azurerm_sql_server.sqlserver.name
}

resource "azurerm_sql_database" "prodsqldatabase" {
  name                = format("%sdatabase-prod",var.deployment_name)
  resource_group_name = var.resource_group_name
  location            = var.location
  server_name         = azurerm_sql_server.sqlserver.name
}