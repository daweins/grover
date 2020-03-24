resource "azurerm_public_ip" "basic_rig_pip" {
 name                         = format("Grover-%s-lb-pip",var.deployment_name)
 location                     = var.location
 resource_group_name          = var.resource_group_name
 allocation_method            = "Static"
}

resource "azurerm_lb" "basic_rig_lb" {
 name                = format("Grover-%s-loadBalancer",var.deployment_name)
 location            = var.location
 resource_group_name = var.resource_group_name

 frontend_ip_configuration {
   name                 = "lbpip"
   public_ip_address_id = azurerm_public_ip.basic_rig_pip.id
 }
}

resource "azurerm_lb_backend_address_pool" "basic_rig_lb_beap" {
 resource_group_name = var.resource_group_name
 loadbalancer_id     = azurerm_lb.basic_rig_lb.id
 name                = "beap"
}

