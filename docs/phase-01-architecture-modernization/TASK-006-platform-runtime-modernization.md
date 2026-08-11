# TASK-006 — Modernize Runtime and Package Baseline

**Status:** Completed
**Phase:** 01 — Solution Architecture & Platform Modernization

## Goal

Move the backend to the approved supported .NET/ASP.NET Core/EF Core baseline and remove obsolete or redundant package choices without combining the upgrade with unrelated feature work.

## Dependencies

- TASK-005 completed.

## Scope

- Select and document the target .NET runtime/SDK version in an ADR or phase note.
- Update target frameworks and compatible first-party packages.
- Review third-party packages for compatibility, duplication, abandonment, or APIs now provided directly by the framework.
- Add a repository-level SDK pin when justified for reproducibility.
- Keep package versions deterministic.
- Resolve upgrade-induced compiler/runtime issues.

## Acceptance Criteria

- [x] All backend projects target the approved runtime consistently unless an explicit exception is documented.
- [x] EF Core provider/tooling versions are compatible with the selected runtime baseline.
- [x] Restore and build succeed from a clean checkout using documented commands.
- [x] Startup succeeds in supported local persistence mode(s).
- [x] Public API behavior is not intentionally changed by the runtime upgrade alone.
- [x] Obsolete package usage discovered during upgrade is removed or documented with a reason to retain it.
- [x] SDK/runtime prerequisites are updated in repository documentation.
- [x] No secrets or machine-local paths are introduced.
- [x] No automated test files are added.

## Verification

Record `dotnet --info`, restore, build, and application-start commands and results. If a package/runtime issue requires a public contract change, block the task and document the conflict rather than silently changing behavior.

## Completion Notes

- Accepted ADR 0002 and moved all four backend projects from `net8.0` to `net10.0`.
- Added `global.json` for the .NET 10.0.1xx SDK feature band with latest-patch roll-forward. Verification selected SDK 10.0.110 and runtime 10.0.10.
- Aligned EF Core, SQL Server provider, and design tooling at 10.0.10; upgraded Swashbuckle, FluentValidation, and Serilog packages to compatible stable versions.
- Removed AutoMapper and replaced two simple maps with explicit Application mapping methods. Removed the unsupported `FluentValidation.AspNetCore` integration while retaining explicit asynchronous validation and DI discovery.
- `dotnet restore UrlShortener.sln --artifacts-path .artifacts/phase01` and `dotnet build UrlShortener.sln --no-restore --artifacts-path .artifacts/phase01` completed with zero warnings and zero errors on 2026-08-11.
- Started the built API in in-memory development mode and verified Swagger (`200`), short-link creation (`201`), and detail retrieval (`200`).
