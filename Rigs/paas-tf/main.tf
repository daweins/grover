provider "azurerm" {
    subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "paas_rig_rg" {
    name = "Grover-paas"
    location = var.location
}

resource "azurerm_storage_account" "storage-acct" {
    name = "mackterraformstg"
    resource_group_name      = "${azurerm_resource_group.paas-rg.name}"
    location                 = var.location
    account_replication_type = "GRS"
    account_tier             = "Standard"
}

resource "azurerm_app_service_plan" "asp" {
  name                = "paas-web-asp"
  location            = var.location
  resource_group_name = "${azurerm_resource_group.paas-rg.name}"

  sku {
    tier = "Standard"
    size = "S1"
  }

}

resource "azurerm_app_service" "as" {
  name                = "pass-web-as"
  location            = var.location
  resource_group_name = "${azurerm_resource_group.paas-rg.name}"
  app_service_plan_id = "${azurerm_app_service_plan.asp.id}"

  site_config {
    dotnet_framework_version = "v4.0"
    scm_type                 = "LocalGit"
  }

  connection_string {
    name  = "Database"
    type  = "SQLServer"
    value = "Server=tcp:${azurerm_sql_server.sqlserver.name}.database.usgovcloudapi.net,1433;Initial Catalog=${azurerm_sql_database.prodsqldatabase.name};Persist Security Info=False;User ID=${azurerm_sql_server.sqlserver.administrator_login};Password=${azurerm_sql_server.sqlserver.administrator_login_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}

resource "random_id" "server" {
  keepers = {
    azi_id = 1
  }

  byte_length = 8
}

resource "azurerm_app_service_slot" "dev" {
  name                = "${random_id.server.hex}-dev"
  app_service_name    = "${azurerm_app_service.as.name}"
  location            = var.location
  resource_group_name = "${azurerm_resource_group.paas-rg.name}"
  app_service_plan_id = "${azurerm_app_service_plan.asp.id}"

  site_config {
    dotnet_framework_version = "v4.0"
  }

  connection_string {
    name  = "Database"
    type  = "SQLServer"
    value = "Server=tcp:${azurerm_sql_server.sqlserver.name}.database.usgovcloudapi.net,1433;Initial Catalog=${azurerm_sql_database.devsqldatabase.name};Persist Security Info=False;User ID=${azurerm_sql_server.sqlserver.administrator_login};Password=${azurerm_sql_server.sqlserver.administrator_login_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}

resource "azurerm_app_service_active_slot" "activeslot" {
  resource_group_name   = "${azurerm_resource_group.paas-rg.name}"
  app_service_name      = "${azurerm_app_service.as.name}"
  app_service_slot_name = "${azurerm_app_service_slot.prod.name}"
}

#Database
resource "random_string" "DatabasePassword" {
  length = 32
  special = true
}

resource "azurerm_sql_server" "sqlserver" {
  name                         = "sqlserver"
  resource_group_name          = "${azurerm_resource_group.paas-rg.name}"
  location                     = var.location
  version                      = "12.0"
  administrator_login          = "4dm1n157r470r"
  administrator_login_password = "${random_string.DatabasePassword.result}"
}

resource "azurerm_sql_database" "devsqldatabase" {
  name                = "devsqlserver"
  resource_group_name = "${azurerm_resource_group.paas-rg.name}"
  location            = var.location
  server_name         = "${azurerm_sql_server.sqlserver.name}"
}

resource "azurerm_sql_database" "prodsqldatabase" {
  name                = "prodsqlserver"
  resource_group_name = "${azurerm_resource_group.paas-rg.name}"
  location            = var.location
  server_name         = "${azurerm_sql_server.sqlserver.name}"
}

output "DatabasePasswordOutput" {
    value = "${random_string.DatabasePassword.*.result}"
}
