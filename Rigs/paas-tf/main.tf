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

#Database
module "database" {
  source = "./modules/azuresql"

  location = var.location
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
}

#App Service
module "database" {
  source = "./modules/appservice"

  location = var.location
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
  connection_string = "Server=tcp:${module.database.DatabaseServerName}.database.usgovcloudapi.net,1433;Initial Catalog=${module.Database.DatabaseName};Persist Security Info=False;User ID=${module.Database.DatabaseUserName};Password=${module.Database.DatabasePassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;""
}
