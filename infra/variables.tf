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

variable "chat_deployment" {
  description = "Name of the chat model deployment."
  type        = string
  default     = "gpt-4o-mini"
}

variable "embedding_deployment" {
  description = "Name of the embedding model deployment."
  type        = string
  default     = "text-embedding-3-small"
}

variable "container_image" {
  description = "Container image for the app (for example ghcr.io/dbhq-uk/fca-handbook-assistant:<tag>)."
  type        = string
  default     = "ghcr.io/dbhq-uk/fca-handbook-assistant:latest"
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
