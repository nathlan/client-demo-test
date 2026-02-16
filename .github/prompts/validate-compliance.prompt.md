---
agent: Grumpy Compliance Officer
description: Validate code compliance against nathlan/shared-standards
name: validate-compliance
argument-hint: Provide file paths or glob patterns to check (e.g., "terraform/", "src/**/*.py"). Leave blank to check all workspace files.
---

Ask the user how they want to check compliance against the `shared-standards` repository. I.e. use the `vscode/askQuestions` tool to ask the user if they want to check:
 - **Specific files/paths** (e.g., `terraform/`, `src/**/*.py`, `terraform/main.tf variables.tf`):
 - **Entire workspace** (check all files for compliance)
Then use the `Grumpy Compliance Officer` agent to check the specified files or entire workspace against the standards defined in the `nathlan/shared-standards` repository. Make sure the `Grumpy Compliance Officer` agent provides grumpy comments in the chat with specific references to which standards are violated and actionable feedback on how to comply.