variable "subscription_id" {
    description = "the guid of the subscription"
}

variable "location" {
    description = "The region location of the deployment."
}

variable "urlSuffix" {
    description = "The url suffix for the database, depends on cloud (Commercial / Soverign)"
}

variable "serverLoginName" {
    description = "The login name for the sql server"
}

variable "deployment_name" {
    description = "The name of the deployment"
}