resource "azurerm_resource_group" "example" {
  name     = "lifelimb-monitor-infr"
  location = "Central US"
}

resource "azurerm_log_analytics_workspace" "lifelimbmonitor" {
  name                = "lalifelimb"
  location            = "${azurerm_resource_group.example.location}"
  resource_group_name = "${azurerm_resource_group.example.name}"
  sku                 = "PerGB2018"
  retention_in_days   = 720
}