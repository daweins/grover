provider "azurerm" {
    subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "basic-rig-rg" {
    name = "Grover"
    location = var.location
}

module "network" {
  source = "./modules"

  address_space = "10.0.0.0/16"
  default_subnet_cidr = "10.0.2.0/24"
  location = var.location
}

resource "azurerm_public_ip" "basic-rig-pip" {
 name                         = "publicIPForLB"
 location                     = azurerm_resource_group.basic-rig-rg.location
 resource_group_name          = azurerm_resource_group.basic-rig-rg.name
 allocation_method            = "Static"
}

resource "azurerm_lb" "basic-rig-lb" {
 name                = "basic-loadBalancer"
 location            = azurerm_resource_group.basic-rig-rg.location
 resource_group_name = azurerm_resource_group.basic-rig-rg.name

 frontend_ip_configuration {
   name                 = "basic-publicIPAddress"
   public_ip_address_id = azurerm_public_ip.basic-rig-pip.id
 }
}

resource "azurerm_lb_backend_address_pool" "basic-rig-lb-beap" {
 resource_group_name = azurerm_resource_group.basic-rig-rg.name
 loadbalancer_id     = azurerm_lb.basic-rig-lb.id
 name                = "basic-BackEndAddressPool"
}

resource "azurerm_network_interface" "basic-rig-vm-nic" {
 count               = var.vmcount
 name                = "basic-nic${count.index}"
 location            = azurerm_resource_group.basic-rig-rg.location
 resource_group_name = azurerm_resource_group.basic-rig-rg.name

 ip_configuration {
   name                          = "testConfiguration"
   subnet_id                     = module.network.subnet_instance_id
   private_ip_address_allocation = "dynamic"
   load_balancer_backend_address_pools_ids = [azurerm_lb_backend_address_pool.basic-rig-lb-beap.id]
 }
 depends_on = [ module.network ]
}

resource "azurerm_availability_set" "avset" {
 name                         = "avset"
 location                     = azurerm_resource_group.basic-rig-rg.location
 resource_group_name          = azurerm_resource_group.basic-rig-rg.name
 platform_fault_domain_count  = var.Fault_Domain_Count
 platform_update_domain_count = var.Update_Domain_Count
 managed                      = true
}

resource "azurerm_virtual_machine" "basic-rig-vm" {
 count                 = var.vmcount
 name                  = "basic-vm${count.index}"
 location              = azurerm_resource_group.basic-rig-rg.location
 availability_set_id   = azurerm_availability_set.avset.id
 resource_group_name   = azurerm_resource_group.basic-rig-rg.name
 network_interface_ids = [element(azurerm_network_interface.basic-rig-vm-nic.*.id, count.index)]
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