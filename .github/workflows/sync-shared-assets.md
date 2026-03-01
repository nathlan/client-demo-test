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
    mode: remote
    toolsets: [actions, issues, repos]
safe-outputs:
  github-token: ${{ secrets.GH_AW_AGENT_TOKEN }}
  upload-asset:
    branch: "assets/sync-shared-assets"
    allowed-exts: [.md, .json, .yml, .yaml]
    max-size: 10240
    max: 50
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
- You have the `safeoutputs` tools: `create_issue` to create issues, and `upload_asset` to upload source files to the `assets/sync-shared-assets` orphaned branch.
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
- Identify lines/blocks present in target but removed from source, and treat those as required deletions.
- Ignore files outside the mapped scope above.
- **Note**: This is a one way sync, we never sync changes back to the `source-repo/sync/` folder.

### Historical intent verification (required)

For every target-only line/block found in mapped paths, you must verify intent using history before deciding whether to preserve or delete:

1. Check local repository history for the target file (for example with `git log -p -- <target-path>` and `git blame <target-path>`).
2. Check upstream history for the mapped source file in `nathlan/shared-assets` using `github` tools (`repos` toolset) and inspect recent commits touching the source path.
3. If upstream history shows that the line/block was removed (or the related input/setting was removed), treat it as intentional upstream deletion and instruct removal downstream.
4. If history is unavailable or ambiguous, default to source-authoritative behavior and remove the target-only line/block.

### Deletion precedence rules (critical)

- The source file in `/tmp/gh-aw/agent/source-repo/sync/**` is authoritative for synced paths.
- If a line/setting exists in target but does not exist in source, it is drift and must be removed unless history verification proves an intentional downstream-only customization that is still valid.
- Do **not** classify a line as "unique local content to preserve" when that line was intentionally removed upstream.

4) **Create Issue**: If you've determined there are changes required in this repository:

- **Upload source files**: For every source file that is new or has changed content, use the `upload_asset` safe output to upload it from its path under `/tmp/gh-aw/agent/source-repo/sync/`.
  - **Important**: The `upload_asset` tool stores files on the asset branch using **content-hash filenames** (e.g. `c82d7f99...db0.json`), not the name you provide. After each upload, record the **actual hash-based filename** returned by the tool — you will need it for the issue instructions.
  - Files without a recognised extension (e.g. `CODEOWNERS`, `.gitignore`) cannot be uploaded via `upload_asset`. For those files, embed their full content directly in the issue body inside a collapsible `<details>` block with a fenced code block.

- **Create the issue** using the `create-issue` safe output.
  - The issue will automatically be assigned to `copilot`.
  - The issue title must start with `[shared-assets-sync]`.
  - The issue body must include:
    - A brief summary of what changed and why.
    - A **file manifest** table with columns: **Asset name** (the hash-based filename returned by `upload_asset`), **Target path** (repo-root-relative), **Action** (`add` | `replace` | `update`).
    - Step-by-step instructions for the Copilot coding agent:
      1. Fetch the asset branch that contains the source files: `git fetch origin assets/sync-shared-assets`
      2. For each file in the manifest, read it from the asset branch using the **hash-based asset name** and write it to the target path: `git show origin/assets/sync-shared-assets:<hash-asset-name> > <target-path>` (e.g. `git show origin/assets/sync-shared-assets:c82d7f99...db0.yml > .github/workflows/foo.yml`)
      3. For files requiring **partial updates** (unique local content to preserve), provide **line-by-line instructions** with the exact content to find and replace. Do not leave anything for the agent to decide.
      4. For files with **deletion** instructions (lines removed upstream), provide explicit find-and-remove instructions with a short `Deletion rationale` section.
      5. For extensionless files embedded directly in the issue body, instruct the agent to write the embedded content verbatim to the target path.
      6. Instructions for `copilot` to update the existing PR title to be start with `[shared-assets-sync] ` followed by a concise description of the change (e.g. "sync upstream changes to .github/workflows/foo.yml").

5) **No Changes**: If you've determined there are no changes required in this repository:

- Call the `noop` safe output exactly once.
- Use a concise message, for example: `No sync changes detected across .github, .devcontainer, and .vscode.`
- Do not create an issue when there are no changes.