variable "project" {
  description = "Short project name used as a prefix for resource names."
  type        = string
  default     = "pos"
}

variable "environment" {
  description = "Deployment environment, for example staging or production."
  type        = string
  default     = "staging"
}

variable "region" {
  description = "AWS region."
  type        = string
  default     = "ap-southeast-1"
}

variable "vpc_cidr" {
  description = "CIDR block of the VPC."
  type        = string
  default     = "10.20.0.0/16"
}

variable "db_instance_class" {
  description = "RDS instance class."
  type        = string
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  description = "RDS storage in GiB."
  type        = number
  default     = 20
}

variable "db_multi_az" {
  description = "Run RDS in Multi-AZ. Set true for production."
  type        = bool
  default     = false
}

variable "db_deletion_protection" {
  description = "Protect the database from deletion. Set true for production."
  type        = bool
  default     = false
}

variable "services" {
  description = "Services that get a container registry repository."
  type        = list(string)
  default     = ["api-gateway", "identity", "catalog", "order", "payment", "notification"]
}
