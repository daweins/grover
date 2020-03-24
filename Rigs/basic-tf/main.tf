provider "azurerm" {
    subscription_id = var.subscription_id
    features {}
}

resource "azurerm_resource_group" "basic_rig_rg" {
    name = format("Grover-%s",var.deployment_name)
    location = var.location
}

module "network" {
  source = "../modules/network"

  address_space = var.address_space
  default_subnet_cidr = var.default_subnet_cidr
  location = var.location
  deployment_name = var.deployment_name
}

module "loadbalancer" {
  source ="../modules/loadbalancer"

  location = var.location
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
  deployment_name = var.deployment_name
}

module "vm" {
  source ="../modules/virtualmachine"

  location = var.location
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
  vmcount = var.vmcount
  Fault_Domain_Count = var.Fault_Domain_Count
  Update_Domain_Count = var.Update_Domain_Count
  adminuser = var.adminuser
  adminpassword = var.adminpassword

  image_publisher = var.image_publisher
  image_offer = var.image_offer
  image_sku = var.image_sku
  image_version = var.image_version

  vm_name_prefix = var.vm_name_prefix
  vm_size = var.vm_size

  vm_os_disk_name = var.vm_os_disk_name
  vm_os_disk_caching = var.vm_os_disk_caching
  vm_os_disk_create_option = var.vm_os_disk_create_option
  vm_os_disk_managed_type = var.vm_os_disk_managed_type

  network_subnet_id = module.network.subnet_instance_id
  loadbalancer_beap_id = module.loadbalancer.lb_beap_id
  deployment_name = var.deployment_name
}