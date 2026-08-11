# TASK-005 — Define and Apply Target Solution Architecture

**Status:** Planned  
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

- [ ] An ADR documents the target architecture, alternatives considered, and reasons for the decision.
- [ ] Domain code does not depend on API/UI/infrastructure implementation concerns.
- [ ] Application use cases depend on abstractions rather than concrete persistence/cache implementations where replacement is required later.
- [ ] Infrastructure implements external/persistence concerns without owning HTTP presentation logic.
- [ ] API remains the HTTP composition boundary.
- [ ] A clear repository location is reserved/documented for Angular and worker processes.
- [ ] Existing URL-shortening behavior still builds after moves/renames.
- [ ] No microservice split is introduced without a separately approved ADR.
- [ ] No automated test files are added.

## Verification

Restore and build the entire solution after restructuring. Record changed project references and any intentionally deferred cleanup in the task completion notes.