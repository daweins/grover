variable "vmcount" {

}

variable "resource_group_name" {
    type = string 
}

variable "location" {
    type = string 
}

variable "Fault_Domain_Count" {

}

variable "Update_Domain_Count" {

}

variable "adminuser" {
    type = string 
}

variable "adminpassword" {
    type = string 
}

variable "image_publisher" {
  type = string 
}

variable "image_offer" {
  type = string 
}

variable "image_sku" {
  type = string 
}

variable "image_version" {
  type = string 
}

variable "vm_name_prefix" {
  type = string 
}

variable "vm_size" {
  type = string
}

variable "vm_os_disk_name" {
  type = string 
}

variable "vm_os_disk_caching" {
  type = string 
}

variable "vm_os_disk_create_option" {
  type = string 
}

variable "vm_os_disk_managed_type" {
  type = string 
}

resource "azurerm_network_interface" "vm_nic" {
 count               = var.vmcount
 name                = "${var.vm_name_prefix}-nic${count.index}"
 location            = var.location
 resource_group_name = var.resource_group_name

 ip_configuration {
   name                          = "testConfiguration"
   subnet_id                     = module.network.subnet_instance_id
   private_ip_address_allocation = "dynamic"
   load_balancer_backend_address_pools_ids = [module.loadbalancer.lb_beap_id]
 }
 
}

resource "azurerm_availability_set" "avset" {
 name                         = "${var.vm_name_prefix}-avset"
 location                     = var.location
 resource_group_name          = var.resource_group_name
 platform_fault_domain_count  = var.Fault_Domain_Count
 platform_update_domain_count = var.Update_Domain_Count
 managed                      = true
}

resource "azurerm_virtual_machine" "vm" {
 count                 = var.vmcount
 name                  = "${var.vm_name_prefix}${count.index}"
 location              = var.location
 availability_set_id   = azurerm_availability_set.avset.id
 resource_group_name   = var.resource_group_name
 network_interface_ids = [element(azurerm_network_interface.vm_nic.*.id, count.index)]
 vm_size               = var.vm_size

 # Uncomment this line to delete the OS disk automatically when deleting the VM
 # delete_os_disk_on_termination = true

 # Uncomment this line to delete the data disks automatically when deleting the VM
 # delete_data_disks_on_termination = true

 storage_image_reference {
   publisher = var.image_publisher
   offer     = var.image_offer
   sku       = var.image_sku
   version   = var.image_version
 }

 storage_os_disk {
   name              = "${var.vm_os_disk_name}${count.index}"
   caching           = var.vm_os_disk_caching
   create_option     = var.vm_os_disk_create_option
   managed_disk_type = varm.vm_os_disk_managed_type
 }

 os_profile {
   computer_name  = "${var.vm_name_prefix}${count.index}"
   admin_username = var.adminuser
   admin_password = var.adminpassword
 }

 os_profile_linux_config {
   disable_password_authentication = false
 }
}