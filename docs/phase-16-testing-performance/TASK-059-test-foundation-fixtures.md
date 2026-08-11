# TASK-059 — Automated Test Foundation and Shared Fixtures

**Status:** Planned  
**Phase:** 16 — Automated Testing & Performance Validation

## Goal

Introduce the automated testing foundation only after the planned product architecture is in place, with reusable fixtures that exercise real boundaries rather than creating a second simplified application architecture for tests.

## Dependencies

- Phase 15 completed.

## Scope

- Add backend test projects with clear separation for unit, architecture, and integration tests.
- Add Angular test/E2E configuration selected for maintainability.
- Add container-backed fixtures for SQL Server, Redis, and selected queue/broker where integration behavior depends on them.
- Create deterministic test configuration/secrets and database reset/seed helpers.
- Define test naming, categories/tags, parallelization, and timeout conventions.
- Ensure production code exposes only justified seams; do not add public APIs solely to make tests convenient.

## Acceptance Criteria

- [ ] Test projects/tools restore and compile from a clean checkout.
- [ ] Integration fixture can start isolated SQL Server, Redis, and broker dependencies or the documented equivalent.
- [ ] Tests do not rely on a developer's pre-existing local database/cache/broker state.
- [ ] Database reset/isolation strategy prevents cross-test data leakage.
- [ ] Test credentials are synthetic and not production secrets.
- [ ] Fast unit/architecture tests can run separately from slower integration/E2E/load suites.
- [ ] Timeouts and cleanup prevent abandoned containers/processes under normal failure.
- [ ] Angular E2E base URL/backend environment is configurable.
- [ ] Repository documentation contains the commands for each test category.

## Verification

Run one minimal passing test in each established category solely to prove the harness/fixture works; substantive coverage belongs to following tasks.