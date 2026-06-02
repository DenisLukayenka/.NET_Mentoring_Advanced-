variable "location" {
  type        = string
  default     = "swedencentral"
  description = "Primary Azure region for all resources."
}

variable "secondary_location" {
  type        = string
  default     = "northeurope"
  description = "Secondary region for geo-replication."
}

variable "project_name" {
  type        = string
  default     = "scheduler"
  description = "Short name used in all resource name prefixes."
}

variable "env" {
  type        = string
  default     = "dev"
  description = "Deployment environment label (e.g. dev, test)."
}

variable "resource_count" {
  type        = number
  default     = 1
  description = "Set to 0 to destroy all resources without removing configuration."
}

variable "documentdb_admin_password" {
  type      = string
  sensitive = true
}
