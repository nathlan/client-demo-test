# ==============================================================================
# TFLint Configuration for ALZ Workload Template
# ==============================================================================
# This is a template repository. Variables are defined for users to utilize
# when they add their own resources. The unused variable warnings are expected
# and suppressed here.
# ==============================================================================

# Concise CLI output 
config {
  format = "compact"
}

# Terraform core linting with recommended preset
plugin "terraform" {
  enabled = true
  preset  = "recommended"
}

# Azure-specific validation rules
plugin "azurerm" {
    enabled = true
    version = "0.30.0"
    source  = "github.com/terraform-linters/tflint-ruleset-azurerm"
}

