# TASK-024 — Distributed Redirect Cache and Invalidation

**Status:** Completed
**Phase:** 06 — Distributed Redirect Cache

## Goal

Replace process-local redirect cache correctness assumptions with a Redis-backed strategy that remains correct across multiple API instances.

## Dependencies

- TASK-023 completed.

## Scope

- Implement the redirect cache through the existing/approved cache abstraction.
- Define the cache model so it contains only fields needed to resolve redirects safely.
- Define TTL for expiring and non-expiring links.
- Invalidate cached redirect state after destination, status, expiry, alias, deletion, restore, or other redirect-affecting mutations.
- Decide whether a small L1 memory cache is retained in front of Redis; if so, document cross-instance invalidation/staleness bounds.
- Avoid caching sensitive management-only data unnecessarily.

## Acceptance Criteria

- [x] Two API instances sharing Redis observe consistent redirect state within the documented invalidation/staleness policy.
- [x] Cached records cannot make an expired/deleted/inactive link redirect beyond the documented tolerance.
- [x] Cache TTL never intentionally exceeds link expiry.
- [x] Redirect-affecting mutations remove/update all relevant cache keys.
- [x] Unknown short codes do not create an unbounded cache-amplification vector; negative caching, if used, is bounded and documented.
- [x] Cache serialization is versionable or deliberately simple enough to evolve safely.
- [x] Management-only/private data is not unnecessarily stored in redirect cache values.
- [x] Backend build succeeds and multi-instance behavior is manually verified.
- [x] No automated test files are added.

## Verification

Run two local API instances against one SQL Server/Redis pair. Warm a code through one instance, mutate it through the other, and verify redirect behavior follows the invalidation contract.

## Completion Notes

- Replaced the process-local `IMemoryCache` adapter with the shared Redis `IDistributedCache`
  provider. Redirect keys are feature-versioned as `redirect:v1:<short-code>` and no L1 cache is
  retained.
- Reduced versioned JSON values to schema version, short URL ID, destination, and expiry. Unknown,
  inactive, deleted, and expired results are not cached.
- Added absolute expiration at the earlier of link expiry or 24 hours. The former one-minute
  minimum, which could outlive a near-term link expiry, was removed.
- Kept post-commit invalidation for destination, expiry, status, deletion, and restore changes.
  The alias remains immutable. The atomic access update now also verifies the exact cached
  destination and expiry, closing stale-fill and failed-invalidation races before a redirect is
  returned.
- Redis connection/timeout failures are logged and treated as cache misses or best-effort
  write/invalidation failures. Persistence remains authoritative, cancellation is preserved, and
  no process-local fallback is introduced. The complete contract is in `docs/redirect-cache.md`.
- `dotnet build UrlShortener.sln` and the isolated-artifacts build both completed with zero
  warnings and zero errors on 2026-08-12.
- Manual verification ran API instances on ports 5140 and 5141 against one LocalDB database and
  Redis namespace. Instance B invalidated update, deactivate, activate, delete, and restore;
  instance A then returned the new destination, `404`, `302`, `404`, and `302`. A deliberately
  reinserted stale value was rejected and replaced before returning the new destination. A
  five-minute link had a 293,913 ms Redis TTL, and an unknown code returned `404` without creating
  a key.
- A third instance using unavailable Redis port 6399 returned the persisted destination with
  `302`, while logging bounded read/write failures. Temporary API processes, Redis keys, access
  logs, link, sessions, and user were removed after verification. No automated test files were
  added.
