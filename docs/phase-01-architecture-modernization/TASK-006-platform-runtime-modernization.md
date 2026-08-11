# TASK-006 — Modernize Runtime and Package Baseline

**Status:** Planned  
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

- [ ] All backend projects target the approved runtime consistently unless an explicit exception is documented.
- [ ] EF Core provider/tooling versions are compatible with the selected runtime baseline.
- [ ] Restore and build succeed from a clean checkout using documented commands.
- [ ] Startup succeeds in supported local persistence mode(s).
- [ ] Public API behavior is not intentionally changed by the runtime upgrade alone.
- [ ] Obsolete package usage discovered during upgrade is removed or documented with a reason to retain it.
- [ ] SDK/runtime prerequisites are updated in repository documentation.
- [ ] No secrets or machine-local paths are introduced.
- [ ] No automated test files are added.

## Verification

Record `dotnet --info`, restore, build, and application-start commands and results. If a package/runtime issue requires a public contract change, block the task and document the conflict rather than silently changing behavior.