# TASK-023 — Redis Infrastructure and Distributed Cache Connection

**Status:** Planned  
**Phase:** 06 — Distributed Redirect Cache

## Goal

Introduce Redis as a supported distributed dependency with explicit configuration and failure behavior, without yet mixing cache-policy refactors into connection setup.

## Dependencies

- Phase 05 completed.

## Scope

- Add the approved Redis client/provider and strongly typed configuration.
- Define connection lifecycle, timeouts, retry boundaries, key-prefix conventions, and environment configuration.
- Add local-development Redis instructions; Docker wiring may be provisional until Phase 15.
- Define application behavior when Redis is temporarily unavailable.
- Avoid placing domain/business logic in the Redis adapter.

## Acceptance Criteria

- [ ] Redis connection settings are environment-configurable and validated.
- [ ] No Redis credentials are committed to source control.
- [ ] Redis client lifetime follows provider guidance and avoids per-request connection creation.
- [ ] Timeouts/retries are bounded and documented.
- [ ] Key namespace/prefix rules prevent ambiguous global keys.
- [ ] Local development can start the backend with a documented Redis instance.
- [ ] A temporary Redis outage has documented application behavior rather than indefinite request hangs.
- [ ] Build and application startup succeed with Redis configured.
- [ ] No automated test files are added.

## Verification

Start Redis, connect the application, verify a basic adapter operation through the intended cache abstraction, then observe/document configured failure behavior with Redis unavailable.