provider "azurerm" {
    subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "grover-rg" {
    name = "Grover"
    location = var.location
}