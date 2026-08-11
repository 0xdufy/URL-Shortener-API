# TASK-061 — Backend Integration, Security, and Concurrency Tests

**Status:** Planned  
**Phase:** 16 — Automated Testing & Performance Validation

## Goal

Verify the real HTTP/persistence/cache/queue behavior against production-like dependencies, especially the correctness boundaries that cannot be proven with mocks.

## Dependencies

- TASK-060 completed.

## Scope

Use the Phase 16 container fixtures and `WebApplicationFactory` or the approved equivalent to cover:

- Registration/sign-in/session/sign-out/revocation.
- Two-user ownership/authorization isolation.
- URL create/list/detail/update/status/delete/restore.
- Generated-code collision and custom-alias uniqueness under concurrency.
- Pagination/filtering/sorting.
- Redis cache hit/miss/invalidation across multiple API instances where practical.
- Distributed rate limiting.
- Idempotency-key behavior.
- Async click publication, duplicate event consumption, worker persistence, eventual analytics.
- API keys/scopes/revocation.
- Custom-domain verification/routing boundaries.
- Security error/validation contracts and secret-leak regressions.

## Acceptance Criteria

- [ ] Integration tests use real SQL Server semantics for uniqueness/index/transaction behavior.
- [ ] Cache tests use real Redis behavior where distributed correctness is under test.
- [ ] Queue/worker tests use the selected broker or an equivalent contract-level fixture justified by the ADR.
- [ ] Concurrent URL creation proves duplicate generated codes cannot create duplicate database rows.
- [ ] Cross-user resource access is denied for every protected operation.
- [ ] Redirect cache invalidation is tested across separate app instances when feasible.
- [ ] Duplicate click event delivery does not double-count.
- [ ] Rate limits are shared across instances.
- [ ] Authentication/API-key secrets are absent from error bodies/log captures used by tests.
- [ ] Integration suite is repeatable from a clean environment.

## Verification

Run the complete integration suite repeatedly and record duration/flaky failures. Any nondeterministic test must be fixed or explicitly quarantined with a blocking issue; do not accept random retries as the permanent solution.