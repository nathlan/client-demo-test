---
name: Sync Shared Assets
description: Sync content from the `nathlan/shared-assets/sync/` directory to the `.github/` directory in this repository.
on:
  schedule: daily between 02:00 utc+12 and 03:00 utc+12 #  Using fuzzy schedule 'daily' instead to distribute workflow execution times. Will run between 3-4 AM during daylight savings
  workflow_dispatch: {}
permissions:
  actions: read
  contents: read
  issues: read
network:
  allowed:
    - defaults
    - github
tools:
  github:
    toolsets: [actions, issues, repos]
engine:
  id: copilot
  steps:
    - name: Checkout source repo to sync from
      uses: actions/checkout@v6
      with:
        repository: nathlan/shared-assets
        token: ${{ secrets.GH_AW_AGENT_TOKEN }}
        path: source-repo
safe-outputs:
  github-token: ${{ secrets.GH_AW_AGENT_TOKEN }}
  create-issue:
    assignees: [copilot]
    title-prefix: "[shared-assets-sync] "
    labels: [agentic-workflow, shared-assets-sync, platform-engineering]
    close-older-issues: true
    max: 1
---

# Sync Shared Assets from Source

This workflow synchronizes the contents of the `.github/` directory in this repository with the `sync/` directory from the `nathlan/shared-assets` repository. It runs daily or can be triggered manually.' This is a one-way sync, from the remote `sync/` directory to this local repository.

## GitHub navigation guide

- **Source repo**: `nathlan/shared-assets`
- **Source sync folder**: `source-repo/sync/` (checked out locally)
- **Target repo**:  This repository where this workflow is running.
- **Target sync folder**: `.github/` (in the current directory)

## Tools

- You have access to `github` tools.
- You have the `safeoutputs` tools to create issues on the current repository.

## Sync Process

1) **Read Source**: Read the contents of the `source-repo/sync/` folder from the local filesystem.
2) **Read Target**: Read the current contents of the `.github/` folder from the local filesystem.
3) **Compare**: Compare the source `source-repo/sync/` folder contents with the local `.github/` contents:

- Identify files that are missing in the local `.github/` folder.
- Identify files that have changed content compared to the source.
- **Note**: This is a one way sync, we never sync changes back to the `source-repo/sync/` folder.

4) **Create Issue**: If you've determined there are changes required in this repository:

- Create an issue in this repository using the `create-issue` safe output.
- The issue must be assigned to `copilot`.
- The issue title must start with `[shared-assets-sync]`.
- The issue body must include:
  - teps for Copilot to perform the update:
    - Provide copilot a list of new files to add locally from the `nathlan/shared-assets` repository. 
      - If you instruct copilot to retrieve the  newfile, make sure it removes the preamble before the code starts. For example, on the first line, the code used to be just `#`, but after copilot processed the file it looked like this: `successfully downloaded text file (SHA: ef9bd9a087ef88a25981e3a00bb335ca5af6ba07)#`
    - For edits to existing files, provide per-line instructions on the files that needs to be updated- be specific and don't leave anything to the copilot cloud coding agent to determine itself.  
  - Explicit instructions to the copilot agent, where:
    - The PR title must start with `[shared-assets-sync] `
    - The PR must have the same labels as the issue i.e. `[agentic-workflow, shared-assets-sync, platform-engineering]`
