variable "resource_count" {
  type        = number
  description = "Set to 0 to destroy all resources without removing configuration."
}

variable "location" {
  type        = string
  description = "Primary Azure region for the cluster."
}

variable "secondary_location" {
  type        = string
  description = "Secondary region for the geo-replica cluster."
}

variable "project_name" {
  type        = string
  description = "Short name used in all resource name prefixes."
}

variable "env" {
  type        = string
  description = "Deployment environment label (e.g. dev, test)."
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group to deploy into."
}

variable "admin_username" {
  type        = string
  default     = "adminuser"
  description = "Administrator username for the MongoDB cluster."
}

variable "admin_password" {
  type        = string
  sensitive   = true
  description = "Administrator password for the MongoDB cluster."
}

variable "storage_size_in_gb" {
  type        = number
  default     = 32
  description = "Storage size in GB per shard."
}

variable "compute_tier" {
  type        = string
  default     = "M30"
  description = "Compute tier for the MongoDB cluster (e.g. M25, M30, M40)."
}

variable "allowed_ip_ranges" {
  type = list(object({
    name     = string
    start_ip = string
    end_ip   = string
  }))
  default     = []
  description = "Additional IP ranges to allow through the MongoDB cluster firewall."
}
