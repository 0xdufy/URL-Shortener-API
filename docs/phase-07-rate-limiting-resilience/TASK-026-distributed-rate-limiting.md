# TASK-026 — Policy-Based Distributed Rate Limiting

**Status:** Planned  
**Phase:** 07 — Distributed Rate Limiting & API Resilience

## Goal

Replace the single-process create limiter with explicit policy-based limiting that behaves predictably across multiple API instances.

## Dependencies

- Phase 06 completed.

## Scope

- Define rate-limit policies for anonymous traffic (if supported), authenticated users, authentication endpoints, URL creation, API keys (future-ready), and redirect traffic where justified.
- Implement distributed counters/state using Redis or another approved mechanism.
- Define fixed/sliding/token-bucket strategy per policy rather than using one algorithm indiscriminately.
- Return consistent `429` errors and standards-compatible retry metadata.
- Make limits configurable with safe bounds.

## Acceptance Criteria

- [ ] Rate-limit identity is explicit per policy: IP, user, API key, or documented combination.
- [ ] Multiple API instances share the same effective limits.
- [ ] Auth endpoints have tighter abuse-oriented policies than normal authenticated management traffic.
- [ ] `429` responses use the common error envelope and include useful retry metadata when computable.
- [ ] Limits are configuration-driven and invalid values fail startup validation or fall back only through documented safe defaults.
- [ ] Redis key expiry prevents unbounded limiter-key retention.
- [ ] Public redirects are not accidentally throttled by URL-creation policy.
- [ ] Build and two-instance manual rate-limit verification succeed.
- [ ] No automated test files are added.

## Verification

Exercise each policy from two API instances and confirm the combined request count respects one distributed limit rather than one limit per process.