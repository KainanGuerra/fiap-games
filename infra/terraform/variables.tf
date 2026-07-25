variable "project_name" {
  description = "Short name used to prefix every resource (e.g. \"fiapgames\")."
  type        = string
  default     = "fiapgames"
}

variable "environment" {
  description = "Deployment environment name (e.g. dev, staging, prod)."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region to deploy into."
  type        = string
  default     = "brazilsouth"
}

variable "container_image" {
  description = "Full image reference to deploy (e.g. ghcr.io/org/fiap-games:latest). Overridden per deploy by CI/CD."
  type        = string
}

variable "container_registry_server" {
  description = "Hostname of the container registry the image is pulled from (e.g. ghcr.io)."
  type        = string
  default     = "ghcr.io"
}

variable "container_registry_username" {
  description = "Username for the external container registry (e.g. a GitHub username or PAT owner)."
  type        = string
}

variable "container_registry_password" {
  description = "Password/token for the external container registry (e.g. a GitHub PAT with read:packages)."
  type        = string
  sensitive   = true
}

variable "jwt_secret" {
  description = "Symmetric secret used to sign JWTs. Must be long and random in production."
  type        = string
  sensitive   = true
}

variable "cosmos_db_free_tier" {
  description = "Whether to use Cosmos DB's free tier (only one per subscription)."
  type        = bool
  default     = false
}

variable "container_cpu" {
  description = "vCPU cores allocated to the API container."
  type        = number
  default     = 0.5
}

variable "container_memory" {
  description = "Memory allocated to the API container (e.g. \"1Gi\")."
  type        = string
  default     = "1Gi"
}

variable "min_replicas" {
  description = "Minimum number of API container replicas."
  type        = number
  default     = 1
}

variable "max_replicas" {
  description = "Maximum number of API container replicas."
  type        = number
  default     = 3
}

variable "tags" {
  description = "Common tags applied to every resource."
  type        = map(string)
  default = {
    project = "fiap-games"
  }
}
