variable "vmcount" {

}

variable "resource_group_name" {
    type = string 
}

variable "deployment_name" {
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

variable "network_subnet_id" {

}

variable "loadbalancer_beap_id" {

}
