resource "azurerm_network_interface" "vm_nic" {
 count               = var.vmcount
 name                = format("Grover-%s-%s-nic%d",var.deployment_name,var.vm_name_prefix,count.index)
 location            = var.location
 resource_group_name = var.resource_group_name

 ip_configuration {
   name                          = "ipconfig"
   subnet_id                     = var.network_subnet_id
   private_ip_address_allocation = "dynamic"
   load_balancer_backend_address_pools_ids = [var.loadbalancer_beap_id]
 }

}

resource "azurerm_availability_set" "avset" {
 name                         = format("Grover-%s-avset",var.deployment_name,var.vm_name_prefix)
 location                     = var.location
 resource_group_name          = var.resource_group_name
 platform_fault_domain_count  = var.Fault_Domain_Count
 platform_update_domain_count = var.Update_Domain_Count
 managed                      = true
}

resource "azurerm_virtual_machine" "vm" {
 count                 = var.vmcount
 name                  = format("Grover-%s-%s-%d",var.deployment_name,var.vm_name_prefix,count.index)
 location              = var.location
 availability_set_id   = azurerm_availability_set.avset.id
 resource_group_name   = var.resource_group_name
 network_interface_ids = [element(azurerm_network_interface.vm_nic.*.id, count.index)]
 vm_size               = var.vm_size

  delete_os_disk_on_termination = true

  delete_data_disks_on_termination = true

 storage_image_reference {
   publisher = var.image_publisher
   offer     = var.image_offer
   sku       = var.image_sku
   version   = var.image_version
 }

 storage_os_disk {
   name              = format("Grover-%s-%s-%d",var.deployment_name,var.vm_os_disk_name,count.index)
   caching           = var.vm_os_disk_caching
   create_option     = var.vm_os_disk_create_option
   managed_disk_type = var.vm_os_disk_managed_type
 }

 os_profile {
   computer_name  = format("Grover-%s-%s-%d",var.deployment_name,var.vm_name_prefix,count.index)
   admin_username = var.adminuser
   admin_password = var.adminpassword
 }

 os_profile_linux_config {
   disable_password_authentication = false
 }
} 