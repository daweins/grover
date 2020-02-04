variable "subscription_id" {
  description = "the guid of the subscription"
  type        = string
}

variable "rg_name" {
    description = "The Resource Group Name"
    type = string
    default = "lifelimb-rig-basictf"
}

variable "location" {
  description = "The region location of the deployment."
  default     = "centralus"
}

variable "vmcount" {
  description = "The number of vms to create"
  default     = "5"
}

variable "adminuser" {
  description = "The admin username for the vm"
  default     = "msadmin"
}

variable "adminpassword" {
  description = "The password for the admin user"
}

variable "Fault_Domain_Count" {
  description = "The number of fault domains"
  default     = "3"
}

variable "Update_Domain_Count" {
  description = "The number of update domains"
  default     = "3"
}

variable "image_publisher" {
  type    = string
  default = "MicrosoftWindowsServer"
}

variable "image_offer" {
  type    = string
  default = "WindowsServer"
}

variable "image_sku" {
  type    = string
  default = "2019-Datacenter"
}

variable "image_version" {
  type    = string
  default = "latest"
}

variable "vm_name_prefix" {
  type    = string
  default = "llbasictf"
}

variable "vm_size" {
  type    = string
  default = "Standard_DS1_v2"
}


variable "address_space" {
  type    = string
  default = "10.0.1.0/24"
}

variable "default_subnet_cidr" {
  type    = string
  default = "10.0.1.0/24"
}

