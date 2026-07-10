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

variable "image_tag" {
  description = "Container image tag to deploy (built into ACR)."
  type        = string
  default     = "latest"
}

variable "embedding_deployment" {
  description = "Name of the embedding model deployment."
  type        = string
  default     = "text-embedding-3-small"
}

variable "postgres_admin_login" {
  description = "Postgres administrator login."
  type        = string
  default     = "fca"
}

variable "postgres_admin_password" {
  description = "Postgres administrator password (supply via TF_VAR_postgres_admin_password; never commit)."
  type        = string
  sensitive   = true
}

variable "allowed_client_ip" {
  description = "Optional public IP allowed to reach Postgres (to run ingestion from a workstation)."
  type        = string
  default     = ""
}
