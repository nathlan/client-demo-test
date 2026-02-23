---
name: Sync Shared Assets
description: Sync content from `nathlan/shared-assets/sync/` into matching repository-root paths in this repository.
on:
  schedule: daily between 02:00 utc+12 and 03:00 utc+12 #  Using fuzzy schedule 'daily' instead to distribute workflow execution times. Will run between 3-4 AM during daylight savings
  workflow_dispatch: {}
permissions:
  actions: read
  contents: read
  issues: read
steps:
  - name: Download source repo archive
    run: |
      mkdir -p /tmp/gh-aw/agent/source-repo
      curl -fsSL \
        -H "Authorization: Bearer ${{ secrets.GH_AW_GITHUB_TOKEN }}" \
        -H "Accept: application/vnd.github+json" \
        "https://api.github.com/repos/nathlan/shared-assets/tarball" \
        -o /tmp/gh-aw/agent/source-repo.tar.gz
      tar -xzf /tmp/gh-aw/agent/source-repo.tar.gz --strip-components=1 -C /tmp/gh-aw/agent/source-repo
      rm -f /tmp/gh-aw/agent/source-repo.tar.gz
network:
  allowed:
    - defaults
    - github
tools:
  github:
    toolsets: [actions, issues, repos]
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

This workflow synchronizes the `sync/` directory from the `nathlan/shared-assets` repository into matching paths at the root of this repository. It runs daily or can be triggered manually. This is a one-way sync, from the remote `sync/` directory to this local repository.

Current sync scope:

- `sync/.github/**` -> `.github/**`
- `sync/.devcontainer/**` -> `.devcontainer/**`
- `sync/.vscode/**` -> `.vscode/**`

File types in scope include all files under the mapped paths, including Markdown and text files such as `.md`, `.mdx`, `.txt`, `.json`, `.yml`, and `.yaml`.

## GitHub navigation guide

- **Source repo**: `nathlan/shared-assets`
- **Source sync folder**: `/tmp/gh-aw/agent/source-repo/sync/` (downloaded locally from archive)
- **Target repo**:  This repository where this workflow is running.
- **Target sync root**: repository root (`./`) where `sync/` subfolders are mapped 1:1

## Tools

- You have access to `github` tools.
- You have the `safeoutputs` tools to create issues on the current repository.
- You also have the `noop` safe output tool for no-change runs.

## Sync Process

1) **Read Source**: Read the contents of the `/tmp/gh-aw/agent/source-repo/sync/` folder from the local filesystem.
2) **Read Target**: Read the current contents of the repository root folders mapped from sync scope (`.github/`, `.devcontainer/`, `.vscode/`).
3) **Compare**: Compare source and target using this path mapping:

- `/tmp/gh-aw/agent/source-repo/sync/.github/**` -> `.github/**`
- `/tmp/gh-aw/agent/source-repo/sync/.devcontainer/**` -> `.devcontainer/**`
- `/tmp/gh-aw/agent/source-repo/sync/.vscode/**` -> `.vscode/**`

- Compare every file type in those paths.
- Identify files that are missing in local target paths.
- Identify files that have changed content compared to source.
- Ignore files outside the mapped scope above.
- **Note**: This is a one way sync, we never sync changes back to the `source-repo/sync/` folder.

4) **Create Issue**: If you've determined there are changes required in this repository:

- Create an issue in this repository using the `create-issue` safe output.
- The issue must be assigned to `copilot`.
- The issue title must start with `[shared-assets-sync]`.
- The issue body must include:
  - Steps for Copilot to perform the update:
    - Provide copilot a list of new files to add/replace locally from `/tmp/gh-aw/agent/source-repo/` (which mirrors `nathlan/shared-assets`). 
      - Include Markdown and documentation updates (`.md`, `.mdx`, `.txt`) when they differ.
      - If you instruct copilot to retrieve the new file, make sure it removes the preamble before the code starts. For example, on the first line, the code used to be just `#`, but after copilot processed the file it looked like this: `successfully downloaded text file (SHA: ef9bd9a087ef88a25981e3a00bb335ca5af6ba07)#`
      - Include each file with explicit source -> target path mapping (for example: `/tmp/gh-aw/agent/source-repo/sync/.vscode/mcp.json` -> `.vscode/mcp.json`).
      - You should only instruct copilot to replace an entire file if the local file has no unique content that is not present in the source file. 
      - If there is unique content in the local file, you should provide line by line instructions to copilot on how to update the file instead of replacing the entire file. Be specific and don't leave anything to the copilot cloud coding agent to determine itself.  
  - Explicit instructions to the copilot agent, where:
    - The PR title must start with `[shared-assets-sync] `
    - The PR must have the same labels as the issue i.e. `[agentic-workflow, shared-assets-sync, platform-engineering]`

5) **No Changes**: If you've determined there are no changes required in this repository:

- Call the `noop` safe output exactly once.
- Use a concise message, for example: `No sync changes detected across .github, .devcontainer, and .vscode.`
- Do not create an issue when there are no changes.