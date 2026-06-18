variable "resource_count" {
  type        = number
  description = "Set to 0 to destroy all resources without removing configuration."
}

variable "location" {
  type        = string
  description = "Primary Azure region for the account."
}

variable "secondary_location" {
  type        = string
  description = "Secondary region for geo-replication."
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

variable "max_throughput" {
  type        = number
  default     = 1000
  description = "Autoscale max RU/s for the scheduler database. Minimum billed: 10% of this value."
}
