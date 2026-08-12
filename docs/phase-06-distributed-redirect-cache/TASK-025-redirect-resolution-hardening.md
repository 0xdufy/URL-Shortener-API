# TASK-025 — Redirect Resolution Hardening

**Status:** Completed
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

- [x] Cache-hit and database-fallback paths apply identical state rules.
- [x] `302`, `404`, and `410` semantics match the documented contract.
- [x] A cache read failure does not incorrectly redirect to stale/unsafe data.
- [x] Valid database fallback can repopulate Redis with the correct TTL.
- [x] Redirect resolution does not require authentication.
- [x] No management authorization/data is evaluated in the public hot path beyond what redirect correctness requires.
- [x] Code structure exposes a clear seam for Phase 08 asynchronous click-event publication.
- [x] Build and manual cache-hit/cache-miss/error verification succeed.
- [x] No automated test files are added.

## Phase 06 Completion Gate

Phase 06 is complete when TASK-023 through TASK-025 are completed and redirect-cache correctness no longer depends on a single API process.

## Completion Notes

- Moved the anonymous public hot path from the authenticated management service to a dedicated
  `IRedirectResolver`. The API now maps explicit `Resolved`, `NotFound`, and `Expired` application
  outcomes to `302`, `404`, and `410`, and `[AllowAnonymous]` documents the route boundary.
- Centralized deleted/inactive/expiry precedence for cache and persistence candidates. A failed
  atomic state guard evicts the candidate and reloads an untracked persisted snapshot up to three
  times, so concurrent expiry and other state changes are reclassified instead of always becoming
  `404`; sustained churn fails closed. Persistence projects only fields required for redirect
  correctness and does not materialize owner or management data.
- Hardened cache values to require an absolute HTTP/HTTPS destination. Malformed/unsupported data
  is evicted, while every non-cancellation cache-provider failure degrades to authoritative
  persistence. Valid fallbacks use the shared absolute-expiration policy and safely repopulate
  Redis.
- Extracted synchronous click/access-log persistence behind `IRedirectAccessRecorder`, preserving
  current analytics behavior while exposing the Phase 08 publication replacement seam. Added a
  debug structured timing/result/source log without introducing metrics or tracing.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors.
  Manual LocalDB/Redis verification covered cache miss, hit, malformed data recovery, valid TTL,
  stale inactive/deleted state, unknown and expired codes, and unavailable Redis fallback. The
  observed results were `302`, `404`, and `410` per contract; four successful requests produced
  four counter/log writes. A final post-projection smoke test repeated miss/hit `302`, TTL, and
  expired `410` checks. Temporary verification state and processes were removed.
- No automated test files were added.
