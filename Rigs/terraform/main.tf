provider "azurerm" {
    subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "basic_rig_rg" {
    name = "Grover"
    location = var.location
}

module "network" {
  source = "./modules"

  address_space = "10.0.0.0/16"
  default_subnet_cidr = "10.0.2.0/24"
  location = var.location
}

module "loadbalancer" {
  source ="./modules"

  location = var.location
  resource_group_name = azurerm_resource_group.basic_rig_rg.name 
}

resource "azurerm_network_interface" "basic_rig_vm_nic" {
 count               = var.vmcount
 name                = "basic-nic${count.index}"
 location            = azurerm_resource_group.basic_rig_rg.location
 resource_group_name = azurerm_resource_group.basic_rig_rg.name

 ip_configuration {
   name                          = "testConfiguration"
   subnet_id                     = module.network.subnet_instance_id
   private_ip_address_allocation = "dynamic"
   load_balancer_backend_address_pools_ids = [module.loadbalancer.lb_beap_id]
 }
 depends_on = [ module.network, module.loadbalancer ]
}

resource "azurerm_availability_set" "avset" {
 name                         = "avset"
 location                     = azurerm_resource_group.basic_rig_rg.location
 resource_group_name          = azurerm_resource_group.basic_rig_rg.name
 platform_fault_domain_count  = var.Fault_Domain_Count
 platform_update_domain_count = var.Update_Domain_Count
 managed                      = true
}

resource "azurerm_virtual_machine" "basic_rig_vm" {
 count                 = var.vmcount
 name                  = "basic_vm${count.index}"
 location              = azurerm_resource_group.basic_rig_rg.location
 availability_set_id   = azurerm_availability_set.avset.id
 resource_group_name   = azurerm_resource_group.basic_rig_rg.name
 network_interface_ids = [element(azurerm_network_interface.basic_rig_vm_nic.*.id, count.index)]
 vm_size               = "Standard_DS1_v2"

 # Uncomment this line to delete the OS disk automatically when deleting the VM
 # delete_os_disk_on_termination = true

 # Uncomment this line to delete the data disks automatically when deleting the VM
 # delete_data_disks_on_termination = true

 storage_image_reference {
   publisher = "Canonical"
   offer     = "UbuntuServer"
   sku       = "16.04-LTS"
   version   = "latest"
 }

 storage_os_disk {
   name              = "osdisk${count.index}"
   caching           = "ReadWrite"
   create_option     = "FromImage"
   managed_disk_type = "Standard_LRS"
 }

 os_profile {
   computer_name  = "webserver${count.index}"
   admin_username = "var.adminuser"
   admin_password = "var.adminpassword"
 }

 os_profile_linux_config {
   disable_password_authentication = false
 }
}