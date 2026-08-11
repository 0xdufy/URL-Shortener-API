# TASK-064 — Continuous Integration Quality Gates

**Status:** Planned  
**Phase:** 17 — CI/CD, Release Engineering & Final Documentation

## Goal

Make repository quality reproducible on every pull request/branch change by running the same build and test gates that define a releasable revision.

## Dependencies

- Phase 16 completed.

## Scope

Add GitHub Actions workflows or the approved CI equivalent for:

- Backend restore/build/format or analyzer checks.
- Angular install/build/lint checks.
- Fast backend tests.
- Container-backed integration tests.
- Angular tests and E2E at an appropriate CI stage.
- Container image build validation.
- Coverage reporting where useful without turning coverage percentage into the sole quality metric.
- Dependency/cache optimization that does not hide stale artifacts.

Load/stress tests may run on demand or on a scheduled/release workflow if full execution is too expensive for every PR.

## Acceptance Criteria

- [ ] CI starts from clean runners and does not depend on developer machine state.
- [ ] Backend and Angular production builds are mandatory gates.
- [ ] Unit/architecture/integration tests are mandatory before release.
- [ ] Critical Angular E2E workflows run automatically before release and on PRs when runtime is acceptable.
- [ ] Failed tests/builds cause a failing workflow rather than being ignored.
- [ ] Container-backed dependencies have health/timeouts and are cleaned up by the runner.
- [ ] Secrets used by CI come from GitHub/environment secret management, not workflow plaintext.
- [ ] CI artifacts/logs avoid exposing generated credentials/API-key secrets.
- [ ] Workflow concurrency/caching settings do not allow an older run to publish over a newer release.

## Verification

Run CI on a clean revision, intentionally demonstrate one safe failing gate on a temporary branch/change if practical, then restore green status and document expected required checks.