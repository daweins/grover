variable "address_space" {
    type = string
    default = "10.0.0.0/16"
}

variable "default_subnet_cidr" {
    type = string 
    default = "10.0.2.0/24"
}

variable "location" {
    type = string
}

variable "deployment_name" {
    type = string
}