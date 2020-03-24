resource "azurerm_app_service_plan" "asp" {
  name                = format("Grover-%s-asp",var.deployment_name)
  location            = var.location
  resource_group_name = var.resource_group_name

  sku {
    tier = "Standard"
    size = "S1"
  }

}

resource "azurerm_app_service" "as" {
  name                = format("Grover-%s-as",var.deployment_name)
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.asp.id

  site_config {
    dotnet_framework_version = "v4.0"
    scm_type                 = "LocalGit"
  }

  connection_string {
    name  = "Database"
    type  = "SQLServer"
    value = var.connection_string
  }
}

resource "random_id" "server" {
  keepers = {
    azi_id = 1
  }

  byte_length = 8
}

resource "azurerm_app_service_slot" "prod" {
  name                = format("Grover-%s-%s-prod",var.deployment_name,random_id.server.hex)
  app_service_name    = azurerm_app_service.as.name
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.asp.id

  site_config {
    dotnet_framework_version = "v4.0"
  }

  connection_string {
    name  = "Database"
    type  = "SQLServer"
    value = var.connection_string 
  }
}

resource "azurerm_app_service_active_slot" "activeslot" {
  resource_group_name   = var.resource_group_name
  app_service_name      = azurerm_app_service.as.name
  app_service_slot_name = azurerm_app_service_slot.prod.name
}