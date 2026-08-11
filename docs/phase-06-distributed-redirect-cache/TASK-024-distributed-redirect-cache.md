# TASK-024 — Distributed Redirect Cache and Invalidation

**Status:** Planned  
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

- [ ] Two API instances sharing Redis observe consistent redirect state within the documented invalidation/staleness policy.
- [ ] Cached records cannot make an expired/deleted/inactive link redirect beyond the documented tolerance.
- [ ] Cache TTL never intentionally exceeds link expiry.
- [ ] Redirect-affecting mutations remove/update all relevant cache keys.
- [ ] Unknown short codes do not create an unbounded cache-amplification vector; negative caching, if used, is bounded and documented.
- [ ] Cache serialization is versionable or deliberately simple enough to evolve safely.
- [ ] Management-only/private data is not unnecessarily stored in redirect cache values.
- [ ] Backend build succeeds and multi-instance behavior is manually verified.
- [ ] No automated test files are added.

## Verification

Run two local API instances against one SQL Server/Redis pair. Warm a code through one instance, mutate it through the other, and verify redirect behavior follows the invalidation contract.