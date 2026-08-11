# TASK-002 — Repository Hygiene and Generated Artifact Cleanup

**Status:** Completed  
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

- [x] No tracked `bin/` or `obj/` files remain unless a documented exception exists.
- [x] `.gitignore` covers all removed generated/machine-local categories.
- [x] Repository still restores and builds successfully after cleanup.
- [x] Required EF migrations and project metadata remain intact.
- [x] No real credentials or private secrets are present in tracked configuration.
- [x] Local-development configuration can be created from documented safe values/templates.
- [x] `git status` after a clean build does not show ignored build artifacts as new untracked files requiring manual cleanup.
- [x] Cleanup does not change runtime behavior.

## Verification

- Clean working tree.
- Restore/build the solution.
- Re-check Git status after build.
- Search tracked files for obvious credential patterns and local absolute paths.

## Completion Gate

The repository can be cloned, restored, built, and left with a clean Git status without committing generated build output.

## Implementation and Verification Record

- Completed 2026-08-11.
- Removed 181 `bin/`/`obj/` files and `UrlShortener.Api.csproj.user` from Git tracking without removing local working copies.
- Expanded `.gitignore` for .NET, IDE, frontend, logs/diagnostics, temporary files, local environment overrides, and OS metadata.
- Preserved EF migrations, project metadata, safe configuration, the SDK pin, and the local tool manifest.
- `dotnet build UrlShortener.sln --no-restore` succeeded with 0 warnings and 0 errors after cleanup.
- A clean build left zero tracked/generated matches, and credential/local-absolute-path scans found no source/configuration secrets or machine paths outside the historical audit evidence.
