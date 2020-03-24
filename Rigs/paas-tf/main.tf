provider "azurerm" {
    subscription_id = var.subscription_id
    features {}
}

resource "azurerm_resource_group" "paas_rig_rg" {
    name = format("Grover-%s-paas",var.deployment_name)
    location = var.location
}

#Database
module "database" {
  source = "../modules/azuresql"

  location = var.location
  resource_group_name = azurerm_resource_group.paas_rig_rg.name
  serverLoginName = var.serverLoginName
  deployment_name = var.deployment_name
}

#App Service
module "appservice" {
  source = "../modules/appservice"

  location = var.location
  resource_group_name = azurerm_resource_group.paas_rig_rg.name
  connection_string = format("Server=tcp:%s.%s,1433;Initial Catalog=%s;Persist Security Info=False;User ID=%s;Password=%s;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",module.database.DatabaseServerName,var.urlSuffix,module.database.DatabaseName,module.database.DatabaseUserName,module.database.DatabasePassword)
  deployment_name = var.deployment_name
}
