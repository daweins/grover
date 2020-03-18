variable "resource_group_name" {
    type = string 
}

variable "location" {
    type = string 
}

resource "random_string" "DatabasePassword" {
  length = 32
  special = true
}

resource "azurerm_sql_server" "sqlserver" {
  name                         = "sqlserver"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = "4dm1n157r470r"
  administrator_login_password = "${random_string.DatabasePassword.result}"
}

resource "azurerm_sql_database" "devsqldatabase" {
  name                = "devsqlserver"
  resource_group_name = var.resource_group_name
  location            = var.location
  server_name         = "${azurerm_sql_server.sqlserver.name}"
}

resource "azurerm_sql_database" "prodsqldatabase" {
  name                = "prodsqlserver"
  resource_group_name = var.resource_group_name
  location            = var.location
  server_name         = "${azurerm_sql_server.sqlserver.name}"
}

output "DatabasePasswordOutput" {
    value = "${random_string.DatabasePassword.*.result}"
}