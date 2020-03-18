variable "location" {
    type = string
}

variable "resource_group_name" {
    type = string 
}

resource "azurerm_public_ip" "basic_rig_pip" {
 name                         = "publicIPForLB"
 location                     = var.location
 resource_group_name          = var.resource_group_name
 allocation_method            = "Static"
}

resource "azurerm_lb" "basic_rig_lb" {
 name                = "basic-loadBalancer"
 location            = var.location
 resource_group_name = var.resource_group_name

 frontend_ip_configuration {
   name                 = "basic-publicIPAddress"
   public_ip_address_id = azurerm_public_ip.basic_rig_pip.id
 }
}

resource "azurerm_lb_backend_address_pool" "basic_rig_lb_beap" {
 resource_group_name = var.resource_group_name
 loadbalancer_id     = azurerm_lb.basic_rig_lb.id
 name                = "basic-BackEndAddressPool"
}

output "lb_beap_id" {
    value = azurerm_lb_backend_address_pool.basic_rig_lb_beap.id
} 