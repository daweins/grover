variable "subscription_id" {
    description = "the guid of the subscription"
}

variable "deployment_name" {
    description = "The name to prefix the deployment"
}

variable "location" {
    description = "The region location of the deployment."
}

variable "vmcount" {
    description = "The number of vms to create"
}

variable "adminuser" {
    description = "The admin username for the vm"
}

variable "adminpassword" {
    description = "The password for the admin user"
}

variable "Fault_Domain_Count" {
    description = "The number of fault domains"
}

variable "Update_Domain_Count" {
    description = "The number of update domains"
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

variable "address_space" {
    type = string 
}

variable "default_subnet_cidr" {
    type = string 
}