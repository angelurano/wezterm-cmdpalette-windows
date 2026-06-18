---
name: git-commit
description: 'Execute git commit using the user-defined brief imperative format (capitalized verb, no feature prefix, issue references without helper keywords).'
license: MIT
allowed-tools: Bash
---

# Git Commit Guidelines

## Overview

Create clean, concise git commits in English following the user's custom format. Analyze the actual diff to determine appropriate changes and summarize them briefly.

## Commit Message Format

```
<CapitalizedVerb> <very brief description of changes> [optional issue reference]
```

### Guidelines
1. **Initial Verb**: Start with a capitalized imperative/infinitive verb (e.g., `Add`, `Fix`, `Refactor`, `Remove`, `Update`, `Implement`).
2. **No Feature Type Prefix**: Do NOT use conventional commit prefixes (such as `feat:`, `fix:`, `refactor:`, `chore:`). Let the initial verb define the type of change.
3. **Very Brief**: Keep the description extremely concise and under 72 characters.
4. **Issue References**: When referencing an issue:
   - Do NOT use words like `for issue #123` or `fixes issue #123`.
   - Simply append the issue reference directly (e.g., `(#123)` or `closes #123`).
5. **Analyze Staged Changes Carefully**: Before writing the commit message, run `git status` and compare staged vs. unstaged files. Distinguish clearly between newly created files (untracked), modified files, and clean/unmodified files to avoid incorrect commit scope assumptions. Ensure all files intended for the logical change (e.g. newly added configuration or rule files like `AGENTS.md`) are properly staged.

### Examples
- **Correct**: `Add unit tests and refactor process spawning (#45)`
- **Correct**: `Fix NullReferenceException in profile loading`
- **Incorrect**: `feat: Add unit tests for issue #45`
- **Incorrect**: `fix: Fix profile loading`

## Workflow

### 1. Analyze Diff

```bash
# If files are staged, use staged diff
git diff --staged

# If nothing staged, use working tree diff
git diff

# Also check status
git status --porcelain
```

### 2. Stage Files (if needed)

If nothing is staged or you want to group changes:

```bash
# Stage specific files
git add path/to/file1 path/to/file2
```

**Never commit secrets** (.env, credentials.json, private keys).

### 3. Execute Commit

```bash
git commit -m "Add unit tests and refactor process spawning (#45)"
```

## Git Safety Protocol

- NEVER update git config.
- NEVER run destructive commands (`--force`, hard reset) without explicit request.
- NEVER skip hooks (`--no-verify`) unless user asks.
- NEVER force push to main/master.
- If commit fails due to hooks, fix and create a NEW commit (don't amend).
