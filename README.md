# [Workload Name] - Azure Landing Zone

> **Note:** This repository was created from the ALZ workload template. Update this README with your workload-specific information.

## Overview

This repository contains the Infrastructure as Code (Terraform) for the `[workload-name]` Azure Landing Zone.

## Repository Structure

```
.
├── .github/
│   └── workflows/
│       └── terraform-deploy.yml    # CI/CD workflow for Terraform
├── terraform/
│   ├── main.tf                     # Main Terraform configuration
│   ├── variables.tf                # Input variables
│   ├── outputs.tf                  # Outputs
│   └── terraform.tf                # Provider and backend config
├── .gitignore                      # Git ignore patterns
└── README.md                       # This file
```

## Deployment Workflow

This repository uses a parent/child workflow pattern:
- **Parent workflow:** `nathlan/.github-workflows/.github/workflows/azure-terraform-deploy.yml` (reusable)
- **Child workflow:** `.github/workflows/terraform-deploy.yml` (this repo)

### Workflow Triggers

- **Pull Requests:** Validates, scans, and plans changes (no apply)
- **Push to main:** Deploys to production with manual approval gate
- **Manual dispatch:** Allows selecting environment for deployment

## Getting Started

### 1. Configure Repository Secrets

Add these secrets in **Settings → Secrets and variables → Actions**:

```
AZURE_CLIENT_ID_PLAN  - User-Assigned Managed Identity Client ID for plan (Reader role)
AZURE_CLIENT_ID_APPLY - User-Assigned Managed Identity Client ID for apply (Owner role)
AZURE_TENANT_ID       - Azure tenant ID
AZURE_SUBSCRIPTION_ID - Azure subscription ID
```

**Security Model: Least Privilege**
- **Plan Identity (Reader):** Used for `terraform init` and `terraform plan` operations. Has read-only access to assess changes.
- **Apply Identity (Owner):** Used for `terraform apply` operations. Has full access to create, modify, and delete resources.

This separation ensures that plan operations cannot accidentally modify infrastructure.

### 2. Create Environment

Create a **production** environment in **Settings → Environments**:
- Enable "Required reviewers" and add platform team members
- Optionally configure deployment branches (e.g., only main)
- Add the same secrets as above at the environment level

### 3. Configure Terraform Backend

Update `terraform/terraform.tf` with your backend configuration:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "stterraformstate"
    container_name       = "tfstate"
    key                  = "[workload-name]-production.tfstate"
    use_oidc             = true
  }
}
```

### 4. Add Your Infrastructure Code

Add your Terraform resources to the `terraform/` directory:
- Use `main.tf` for resource definitions
- Define variables in `variables.tf`
- Expose outputs in `outputs.tf`

### 5. Create a Pull Request

1. Create a feature branch
2. Add your Terraform changes
3. Push and create a PR
4. Review the Terraform plan in PR comments
5. Get approval from the platform team
6. Merge to trigger deployment

## Azure OIDC Setup

This repository uses **User-Assigned Managed Identities (UAMIs)** with federated credentials for secure, passwordless authentication to Azure.

### Setup Two Managed Identities

You need to create TWO separate managed identities with federated credentials:

#### 1. Plan Identity (Reader Role)

```bash
# Create User-Assigned Managed Identity for plan operations
RESOURCE_GROUP="rg-github-identities"
PLAN_IDENTITY_NAME="uami-github-${REPO_NAME}-plan"

az identity create \
  --resource-group $RESOURCE_GROUP \
  --name $PLAN_IDENTITY_NAME

# Get the client ID
PLAN_CLIENT_ID=$(az identity show \
  --resource-group $RESOURCE_GROUP \
  --name $PLAN_IDENTITY_NAME \
  --query clientId -o tsv)

# Assign Reader role at subscription scope
az role assignment create \
  --assignee $PLAN_CLIENT_ID \
  --role Reader \
  --scope "/subscriptions/${SUBSCRIPTION_ID}"

# Add federated credential for GitHub Actions
az identity federated-credential create \
  --identity-name $PLAN_IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --name "github-${REPO_NAME}-plan" \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:nathlan/${REPO_NAME}:environment:production" \
  --audiences "api://AzureADTokenExchange"
```

#### 2. Apply Identity (Owner Role)

```bash
# Create User-Assigned Managed Identity for apply operations
APPLY_IDENTITY_NAME="uami-github-${REPO_NAME}-apply"

az identity create \
  --resource-group $RESOURCE_GROUP \
  --name $APPLY_IDENTITY_NAME

# Get the client ID
APPLY_CLIENT_ID=$(az identity show \
  --resource-group $RESOURCE_GROUP \
  --name $APPLY_IDENTITY_NAME \
  --query clientId -o tsv)

# Assign Owner role at subscription scope
az role assignment create \
  --assignee $APPLY_CLIENT_ID \
  --role Owner \
  --scope "/subscriptions/${SUBSCRIPTION_ID}"

# Add federated credential for GitHub Actions
az identity federated-credential create \
  --identity-name $APPLY_IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --name "github-${REPO_NAME}-apply" \
  --issuer "https://token.actions.githubusercontent.com" \
  --subject "repo:nathlan/${REPO_NAME}:environment:production" \
  --audiences "api://AzureADTokenExchange"
```

#### 3. Add Client IDs to GitHub Secrets

```bash
# Add the plan identity client ID
gh secret set AZURE_CLIENT_ID_PLAN --body "$PLAN_CLIENT_ID" --repo nathlan/${REPO_NAME}

# Add the apply identity client ID
gh secret set AZURE_CLIENT_ID_APPLY --body "$APPLY_CLIENT_ID" --repo nathlan/${REPO_NAME}
```

### Benefits of UAMIs

- ✅ **No stored credentials** - Client IDs are not sensitive
- ✅ **Least privilege** - Plan uses read-only, apply uses elevated permissions
- ✅ **Audit trail** - Each identity's actions tracked separately in Azure
- ✅ **Defense in depth** - Compromised plan job cannot modify infrastructure

## Support

For questions or issues:
- Create an issue in this repository
- Contact the platform engineering team
- Reference the ALZ vending documentation in `nathlan/.github-private`

## Related Repositories

- **ALZ Subscriptions:** `nathlan/alz-subscriptions` - Subscription vending infrastructure
- **Reusable Workflows:** `nathlan/.github-workflows` - Central workflow definitions
- **LZ Vending Module:** `nathlan/terraform-azurerm-landing-zone-vending`