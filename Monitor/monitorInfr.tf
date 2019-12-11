variable "aksSPID" {
  type = string
}

variable "aksSPPwd" {
  type = string
}


resource "azurerm_resource_group" "main" {
  name     = "lifelimb-monitor-infr"
  location = "Central US"
}

resource "azuread_service_principal" "llakssp" {
  application_id = "${var.aksSPID}"
}
resource "azuread_service_principal_password" "llakssp" {
  service_principal_id = "${var.aksSPID}"
  value                = "${var.aksSPPwd}"
  end_date             = "2021-01-01T01:02:03Z"
}

resource "azurerm_log_analytics_workspace" "lifelimbmonitor" {
  name                = "lalifelimb"
  location            = "${azurerm_resource_group.main.location}"
  resource_group_name = "${azurerm_resource_group.main.name}"
  sku                 = "PerGB2018"
  retention_in_days   = 720
}


resource "azurerm_kubernetes_cluster" "llaks" {
  name                = "lifelimb-aks"
  location            = "${azurerm_resource_group.main.location}"
  resource_group_name = "${azurerm_resource_group.main.name}"
  dns_prefix          = "lifelimbaks"

  default_node_pool {
    name       = "default"
    node_count = 1
    vm_size    = "Standard_D2_v2"
  }

  service_principal {
    client_id     = "${azuread_service_principal.llakssp.application_id}"
    client_secret = "${var.aksSPPwd}"
  }

  
}

output "client_certificate" {
  value = azurerm_kubernetes_cluster.llaks.kube_config.0.client_certificate
}

output "kube_config" {
  value = azurerm_kubernetes_cluster.llaks.kube_config_raw
}