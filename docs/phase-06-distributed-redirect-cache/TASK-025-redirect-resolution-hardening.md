# TASK-025 — Redirect Resolution Hardening

**Status:** Planned  
**Phase:** 06 — Distributed Redirect Cache

## Goal

Refine redirect resolution around the new distributed cache so cache hits, database fallbacks, link state evaluation, and failure handling have one clear contract.

## Dependencies

- TASK-024 completed.

## Scope

- Consolidate redirect-state evaluation so cache and database paths cannot drift semantically.
- Define explicit behavior for unknown, deleted, inactive, and expired codes.
- Ensure database fallback repopulates cache safely.
- Ensure cache corruption/deserialization failure degrades in a controlled way.
- Keep analytics-write behavior unchanged until Phase 08, while preparing a clean boundary for later event emission.
- Add internal timing/logging hooks only where they do not prematurely implement Phase 14 telemetry.

## Acceptance Criteria

- [ ] Cache-hit and database-fallback paths apply identical state rules.
- [ ] `302`, `404`, and `410` semantics match the documented contract.
- [ ] A cache read failure does not incorrectly redirect to stale/unsafe data.
- [ ] Valid database fallback can repopulate Redis with the correct TTL.
- [ ] Redirect resolution does not require authentication.
- [ ] No management authorization/data is evaluated in the public hot path beyond what redirect correctness requires.
- [ ] Code structure exposes a clear seam for Phase 08 asynchronous click-event publication.
- [ ] Build and manual cache-hit/cache-miss/error verification succeed.
- [ ] No automated test files are added.

## Phase 06 Completion Gate

Phase 06 is complete when TASK-023 through TASK-025 are completed and redirect-cache correctness no longer depends on a single API process.