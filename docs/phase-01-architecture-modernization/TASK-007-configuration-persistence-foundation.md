# TASK-007 — Configuration and Persistence Foundation Hardening

**Status:** Completed
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

- [x] Important settings are represented by typed options or an equally explicit validated configuration model.
- [x] Missing required non-development configuration fails fast with an actionable error.
- [x] Connection strings/secrets are not committed as production credentials.
- [x] SQL Server migration procedure is documented and repeatable.
- [x] Application startup does not silently create or mutate production schema outside the documented migration strategy.
- [x] Retry behavior is bounded and does not hide persistent configuration failure.
- [x] In-memory persistence, if retained, is explicitly marked as non-production and behavior differences are documented.
- [x] Build and local startup succeed after the configuration changes.
- [x] No automated test files are added.

## Phase 01 Completion Gate

Phase 01 is complete when TASK-005 through TASK-007 are completed, the approved architecture/runtime are documented, the solution builds cleanly, and configuration/persistence startup is ready for Phase 02 identity work.

## Completion Notes

- Added validated `StorageOptions`, `PersistenceOptions`, and `RateLimitingOptions`; the in-memory rate limiter now consumes typed options.
- Production defaults to SQL Server and fails immediately when its connection string is missing. In-memory storage is rejected outside Development with an actionable options-validation error.
- Moved the LocalDB example to development-only configuration and removed the unused cache setting. No shared or production credential is committed.
- Bounded SQL transient retries to 0-10 attempts, retry delay to 1-60 seconds, and command timeout to 1-300 seconds.
- Added the repository-local EF Core 10.0.10 tool manifest, an `InitialCreate` migration, model snapshot, migration convention, operator-run application procedure, and in-memory limitations. API startup contains no automatic schema mutation.
- `dotnet restore UrlShortener.sln --artifacts-path .artifacts/phase01` and `dotnet build UrlShortener.sln --no-restore --artifacts-path .artifacts/phase01` completed with zero warnings and zero errors on 2026-08-11.
- EF reported no pending model changes and generated a 2,892-byte idempotent SQL script containing both tables and the unique short-code index.
- Development in-memory startup returned Swagger `200` and create `201`. An isolated LocalDB database applied the migration and returned create `201` plus details `200`; the verification database was then successfully dropped.
- The Phase 01 implementation gate is satisfied. The roadmap's separate Phase 00 task files are still marked Planned because the user explicitly directed Phase 01 to begin before that prerequisite was recorded complete.
