resource "azurerm_resource_group" "basic_rig_network_rg" {
    name = format("Grover-%s-Network",var.deployment_name)
    location = var.location
}

resource "azurerm_virtual_network" "basic_rig_vnet" {
    name                = format("Grover-%s-vnet",var.deployment_name)
    address_space       = [var.address_space]
    location            = azurerm_resource_group.basic_rig_network_rg.location
    resource_group_name = azurerm_resource_group.basic_rig_network_rg.name
}

resource "azurerm_subnet" "basic_rig_subnet" {
 name                 = format("Grover-%s-subnet",var.deployment_name)
 resource_group_name  = azurerm_resource_group.basic_rig_network_rg.name
 virtual_network_name = azurerm_virtual_network.basic_rig_vnet.name
 address_prefix       = var.default_subnet_cidr
}

