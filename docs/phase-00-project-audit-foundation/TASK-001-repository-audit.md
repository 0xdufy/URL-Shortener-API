# TASK-001 — Repository Audit and Baseline Inventory

**Status:** Completed  
**Phase:** 00 — Project Audit & Engineering Foundation

## Goal

Create an evidence-based baseline of the current repository before structural or platform changes. The output of this task must make later decisions traceable to the actual codebase rather than assumptions.

## Scope

- Inventory projects, project references, runtime targets, NuGet dependencies, configuration files, EF Core migrations, controllers/endpoints, domain entities, application services, infrastructure adapters, middleware, caching, rate limiting, logging, and generated/tracked artifacts.
- Record the current public API surface and current error/status-code behavior.
- Record current persistence modes and startup requirements.
- Identify build artifacts or machine-local files already tracked by Git.
- Identify obvious correctness, maintainability, security, concurrency, and performance risks without fixing unrelated code in this task.
- Identify current build warnings/errors using the documented supported environment.

## Required Deliverable

Create `docs/phase-00-project-audit-foundation/audit-report.md` containing:

1. Current solution/project map.
2. Current dependency direction.
3. Current endpoint inventory.
4. Current data model and important indexes.
5. Current cache/rate-limit behavior.
6. Current redirect write path.
7. Current configuration/secrets assessment.
8. Tracked generated-artifact assessment.
9. Risk register with severity (`Critical`, `High`, `Medium`, `Low`).
10. Recommended items that must be resolved before Phase 01 begins.

## Out of Scope

- No architecture rewrite.
- No runtime upgrade.
- No authentication implementation.
- No automated test files.
- No feature additions.

## Acceptance Criteria

- [x] Audit report is based on inspected repository content, not README assumptions alone.
- [x] Every project and its direct project references are documented.
- [x] Every currently exposed API endpoint is listed with method and route.
- [x] Existing persistence entities and unique/index constraints relevant to URL lookup are documented.
- [x] Existing `IMemoryCache` and in-memory rate-limiter assumptions are documented.
- [x] The synchronous redirect analytics write path is explicitly documented.
- [x] Tracked `bin/`, `obj/`, logs, secrets, or other generated/local artifacts are identified by path if present.
- [x] Build command and observed result are recorded.
- [x] Risks include evidence/path references and recommended disposition.
- [x] No production code behavior is changed by this task.

## Verification

Run the repository build and any non-test static checks already present. Record the exact commands and outcomes in the audit report.

## Completion Gate

This task is complete only when another engineer can read the audit report and understand the current technical baseline well enough to review Phase 01 design decisions.

## Implementation and Verification Record

- Completed 2026-08-11.
- Added `audit-report.md` based on the inspected working tree, project files, source, EF configuration/migration, configuration, tracked-file inventory, and dependency inventory.
- Restored and built `UrlShortener.sln` with .NET SDK 10.0.110: 0 warnings and 0 errors.
- Recorded the sandboxed NuGet failure separately from the successful network-enabled restore.
