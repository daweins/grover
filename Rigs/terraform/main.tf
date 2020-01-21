provider "azurerm" {
    subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "basic_rig_rg" {
    name = "Grover"
    location = var.location
}

module "network" {
  source = "./modules/network"

  address_space = "10.0.0.0/16"
  default_subnet_cidr = "10.0.2.0/24"
  location = var.location
}

module "loadbalancer" {
  source ="./modules/loadbalancer"

  location = var.location
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
}

module "vm" {
  source ="./modules/virtualmachine"

  location = var.location
  vmcount = var.vmcount

  # Put in input variables

  depends_on = [ module.network, module.loadbalancer ]
}