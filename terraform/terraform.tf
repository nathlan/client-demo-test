# ==============================================================================
# Terraform and Provider Configuration
# ==============================================================================

# terraform {
#   required_version = ">= 1.9.0"

#   # Configure Azure backend for remote state
#   # UPDATE THIS with your actual backend configuration
#   backend "azurerm" {
#     resource_group_name  = "rg-terraform-state"
#     storage_account_name = "stterraformstate"
#     container_name       = "tfstate"
#     key                  = "workload-template.tfstate" # UPDATE: Change to your workload name
#     use_oidc             = true
#   }

#   required_providers {
#     azurerm = {
#       source  = "hashicorp/azurerm"
#       version = "~> 4.0"
#     }
#   }
# }

# provider "azurerm" {
#   features {}
#   use_oidc = true
# }
