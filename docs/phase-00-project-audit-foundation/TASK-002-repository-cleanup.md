# TASK-002 — Repository Hygiene and Generated Artifact Cleanup

**Status:** Planned  
**Phase:** 00 — Project Audit & Engineering Foundation

## Goal

Make the repository clean and reproducible by removing generated or machine-local artifacts from Git tracking and ensuring future builds do not reintroduce them.

## Dependencies

- TASK-001 completed and generated-artifact findings available.

## Scope

- Remove tracked `bin/`, `obj/`, build output, IDE state, logs, temporary files, and other generated artifacts identified by TASK-001.
- Keep source files, migrations, intentional generated source, lock files, and configuration templates that are required for reproducible builds.
- Review and strengthen `.gitignore` for the actual repository layout.
- Verify no committed connection-string secrets, tokens, passwords, API keys, local absolute paths, or machine-specific state remain.
- Preserve user-facing documentation and intentionally committed sample configuration.

## Implementation Requirements

- Do not delete a file merely because it is generated-looking; first determine whether the project intentionally relies on it.
- If sensitive material is discovered in Git history, document it as a security finding. Do not pretend deleting the current file removes historical exposure.
- Do not rewrite Git history in this task unless explicitly approved separately.

## Acceptance Criteria

- [ ] No tracked `bin/` or `obj/` files remain unless a documented exception exists.
- [ ] `.gitignore` covers all removed generated/machine-local categories.
- [ ] Repository still restores and builds successfully after cleanup.
- [ ] Required EF migrations and project metadata remain intact.
- [ ] No real credentials or private secrets are present in tracked configuration.
- [ ] Local-development configuration can be created from documented safe values/templates.
- [ ] `git status` after a clean build does not show ignored build artifacts as new untracked files requiring manual cleanup.
- [ ] Cleanup does not change runtime behavior.

## Verification

- Clean working tree.
- Restore/build the solution.
- Re-check Git status after build.
- Search tracked files for obvious credential patterns and local absolute paths.

## Completion Gate

The repository can be cloned, restored, built, and left with a clean Git status without committing generated build output.