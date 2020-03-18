provider "azurerm" {
    subscription_id = var.subscription_id
}

resource "azurerm_resource_group" "paas_rig_rg" {
    name = "Grover-paas"
    location = var.location
}

resource "azurerm_storage_account" "storage-acct" {
    name = "mackterraformstg"
    resource_group_name      = "${azurerm_resource_group.paas-rg.name}"
    location                 = var.location
    account_replication_type = "GRS"
    account_tier             = "Standard"
}

#Database

