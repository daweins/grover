output "name" {
    value = "BackendNetwork"
}

output "subnet_instance_id" {
    value = azurerm_subnet.basic_rig_subnet.id
}

output "networkrg_name" {
    value = azurerm_resource_group.basic_rig_network_rg.name
} 