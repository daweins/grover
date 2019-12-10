variable "subscription_id" {
    description = "the guid of the subscription"
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