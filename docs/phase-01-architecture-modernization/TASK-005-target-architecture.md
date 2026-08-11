# TASK-005 — Define and Apply Target Solution Architecture

**Status:** Completed
**Phase:** 01 — Solution Architecture & Platform Modernization

## Goal

Restructure the solution only where the Phase 00 audit demonstrates a concrete need, producing clear boundaries for the API, application/domain logic, infrastructure, Angular client, and future background worker.

## Dependencies

- Phase 00 completed.

## Scope

- Write an ADR describing the chosen modular-monolith structure and dependency rules.
- Decide whether existing `Domain`, `Application`, `Infrastructure`, and `Api` projects remain, are renamed, or are reorganized.
- Reserve explicit locations for `web`/Angular and worker code without prematurely implementing later-phase features.
- Move code only when necessary to make ownership/dependency boundaries clear.
- Remove circular or inappropriate project references.

## Acceptance Criteria

- [x] An ADR documents the target architecture, alternatives considered, and reasons for the decision.
- [x] Domain code does not depend on API/UI/infrastructure implementation concerns.
- [x] Application use cases depend on abstractions rather than concrete persistence/cache implementations where replacement is required later.
- [x] Infrastructure implements external/persistence concerns without owning HTTP presentation logic.
- [x] API remains the HTTP composition boundary.
- [x] A clear repository location is reserved/documented for Angular and worker processes.
- [x] Existing URL-shortening behavior still builds after moves/renames.
- [x] No microservice split is introduced without a separately approved ADR.
- [x] No automated test files are added.

## Verification

Restore and build the entire solution after restructuring. Record changed project references and any intentionally deferred cleanup in the task completion notes.

## Completion Notes

- Accepted ADR 0001 and retained the existing four backend projects because their references are already acyclic and correctly directed.
- Added reserved, documentation-only `web/` and `workers/` locations. Angular and worker scaffolding remain deferred to their roadmap phases.
- No source moves, renames, or project-reference changes were necessary.
- Verified with `dotnet restore UrlShortener.sln --artifacts-path .artifacts/phase01` and `dotnet build UrlShortener.sln --no-restore --artifacts-path .artifacts/phase01`: build succeeded with zero errors on 2026-08-11.
- The repository's Phase 00 task files remain marked Planned. TASK-005 was begun by explicit user direction; this note does not retroactively claim the Phase 00 completion gate was satisfied.
