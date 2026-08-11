# TASK-007 — Configuration and Persistence Foundation Hardening

**Status:** Planned  
**Phase:** 01 — Solution Architecture & Platform Modernization

## Goal

Make application configuration and persistence startup explicit, safe, and suitable for future authentication, Redis, workers, and containerized deployment.

## Dependencies

- TASK-006 completed.

## Scope

- Define strongly typed options for important application settings.
- Validate required production-like settings during startup.
- Define safe local-development defaults/templates without committing real credentials.
- Review EF Core DbContext registration, retry behavior, migration startup policy, connection handling, and SQL-specific configuration.
- Define a migration naming/application convention.
- Decide whether in-memory persistence remains a supported development mode; document limitations if retained.

## Acceptance Criteria

- [ ] Important settings are represented by typed options or an equally explicit validated configuration model.
- [ ] Missing required non-development configuration fails fast with an actionable error.
- [ ] Connection strings/secrets are not committed as production credentials.
- [ ] SQL Server migration procedure is documented and repeatable.
- [ ] Application startup does not silently create or mutate production schema outside the documented migration strategy.
- [ ] Retry behavior is bounded and does not hide persistent configuration failure.
- [ ] In-memory persistence, if retained, is explicitly marked as non-production and behavior differences are documented.
- [ ] Build and local startup succeed after the configuration changes.
- [ ] No automated test files are added.

## Phase 01 Completion Gate

Phase 01 is complete when TASK-005 through TASK-007 are completed, the approved architecture/runtime are documented, the solution builds cleanly, and configuration/persistence startup is ready for Phase 02 identity work.