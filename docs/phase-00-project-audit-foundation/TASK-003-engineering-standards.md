# TASK-003 — Engineering Standards and Repository Conventions

**Status:** Completed  
**Phase:** 00 — Project Audit & Engineering Foundation

## Goal

Define the minimum engineering conventions Codex and human contributors must follow before the project grows across backend, Angular, workers, infrastructure, and documentation.

## Dependencies

- TASK-001 completed.
- TASK-002 completed or its remaining exceptions documented.

## Scope

Create `docs/engineering-standards.md` covering:

- Repository/project naming conventions.
- C# formatting, nullable-reference handling, async/cancellation conventions, and warning policy.
- Angular naming, folder, component/service, typing, and API-client conventions.
- Dependency direction and where business logic may live.
- Configuration and secret-management rules.
- Database migration rules.
- API versioning and error-contract rules.
- Logging rules, especially prohibited secret/sensitive-data logging.
- Task status/update rules.
- Commit/review scope expectations.
- Documentation/ADR rules for material decisions.

Add repository-level configuration only where it provides enforceable value, such as `.editorconfig` or common build properties. Avoid cosmetic rules that generate churn without improving correctness/readability.

## Required Decisions

- Whether warnings are promoted to errors globally now or later.
- Whether central package management is adopted in Phase 01.
- How frontend/backend formatting tools are invoked consistently.
- Which generated files are intentionally committed.

## Acceptance Criteria

- [x] `docs/engineering-standards.md` exists and is specific to this repository.
- [x] Backend and Angular conventions are both covered.
- [x] Dependency-direction rules agree with the PRD and do not force unnecessary abstractions.
- [x] Async I/O methods are required to propagate cancellation when meaningful.
- [x] Date/time conventions require UTC at persistence/API boundaries unless explicitly documented otherwise.
- [x] Secret/logging rules explicitly forbid passwords, session tokens, refresh tokens, API-key secrets, and authorization headers in logs.
- [x] Migration rules prohibit editing already-applied production migrations without an explicit migration strategy.
- [x] Public contract changes require documentation and review.
- [x] Any added formatter/analyzer configuration builds successfully with the current codebase or exceptions are documented for Phase 01.
- [x] No automated test files are introduced.

## Verification

Apply formatting/build commands defined by the new standard to the current repository and record any pre-existing violations that cannot safely be resolved inside this task.

## Completion Gate

Future Codex tasks have one explicit set of repository conventions to follow instead of inferring style and architecture independently.

## Implementation and Verification Record

- Completed 2026-08-11.
- Added `docs/engineering-standards.md` with backend, Angular, dependency, configuration, migration, API, logging, generated-file, task/review, ADR, package, and tool conventions.
- Added a low-churn root `.editorconfig` for encoding, whitespace, indentation, and core C# conventions.
- Kept warnings-as-errors and central package management deferred to Phase 01 while requiring a zero-warning baseline now.
- `dotnet format UrlShortener.sln --verify-no-changes --no-restore` succeeded and formatted 0 of 56 files.
