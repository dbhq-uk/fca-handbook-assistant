variable "subscription_id" {
  description = "The Azure subscription to deploy the assistant's resources into."
  type        = string
}

variable "workload" {
  description = "Short workload/application token used in resource names (CAF naming)."
  type        = string
  default     = "fca"
}

variable "environment" {
  description = "Environment token used in resource names (dev, test, prod)."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region to deploy into (for example uksouth)."
  type        = string
  default     = "uksouth"
}
