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
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
  vmcount = var.vmcount
  Fault_Domain_Count = var.Fault_Domain_Count
  Update_Domain_Count = var.Update_Domain_Count
  adminuser = var.adminuser
  adminpassword = var.adminpassword

  image_publisher = "Canonical"
  image_offer = "UbuntuServer"
  image_sku = "16.04-LTS"
  image_version = "latest"

  vm_name_prefix = "basic_vm"
  vm_size = "Standard_DS1_v2"

  vm_os_disk_name = "osdisk"
  vm_os_disk_caching = "ReadWrite"
  vm_os_disk_create_option = "FromImage"
  vm_os_disk_managed_type = "Standard_LRS"

  # Put in input variables

  depends_on = [ module.network, module.loadbalancer ]
}