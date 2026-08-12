# TASK-026 — Policy-Based Distributed Rate Limiting

**Status:** Completed
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

- [x] Rate-limit identity is explicit per policy: IP, user, API key, or documented combination.
- [x] Multiple API instances share the same effective limits.
- [x] Auth endpoints have tighter abuse-oriented policies than normal authenticated management traffic.
- [x] `429` responses use the common error envelope and include useful retry metadata when computable.
- [x] Limits are configuration-driven and invalid values fail startup validation or fall back only through documented safe defaults.
- [x] Redis key expiry prevents unbounded limiter-key retention.
- [x] Public redirects are not accidentally throttled by URL-creation policy.
- [x] Build and two-instance manual rate-limit verification succeed.
- [x] No automated test files are added.

## Verification

Exercise each policy from two API instances and confirm the combined request count respects one distributed limit rather than one limit per process.

## Completion Notes

- Replaced both process-local limiters with endpoint-selected fixed-window, sliding-window, and
  token-bucket policies evaluated atomically in Redis. Redis server time coordinates instances;
  SHA-256 partition keys keep raw IP, user, and future API-key identifiers out of key names.
- Applied direct-peer-IP policies to anonymous/authentication endpoints, JWT-subject policies to
  authenticated management and URL creation, and reserved an `api_key_id` token-bucket policy for
  Phase 10. Public redirects intentionally have no limiter metadata.
- Shared one lazy StackExchange.Redis multiplexer between distributed caching and rate limiting.
  Fixed/sliding/token state uses bounded TTLs, and Redis failures fail closed for limited endpoints
  as `503 RATE_LIMITING_UNAVAILABLE` without affecting public redirects.
- Normalized rejected requests to the common `429 RATE_LIMITED` envelope with integer-seconds
  `Retry-After` and `Cache-Control: no-store`. Nested policy settings are configuration-bound and
  startup-validated with documented bounds.
- On 2026-08-12, two API processes on ports 5101 and 5102 shared one SQL database, signing key, and
  Redis namespace. Alternating requests produced combined sequences of `200,200,429` (anonymous),
  `201,409,429` (registration), `200,200,429` (sign-in), `400,400,429` (session policy),
  `201,201,429` (creation), and `200,200,200,429` (authenticated management). Five alternating
  public redirect requests remained `302` after management and creation limits were exhausted.
- Redis inspection observed positive TTLs on fixed-window hashes, sliding-window sorted sets, and
  token-bucket hashes. An invalid zero-capacity override failed startup validation. Stopping Redis
  returned the documented `503`, and the shared multiplexer recovered after restart.
- `dotnet format UrlShortener.sln --verify-no-changes --no-restore` and
  `dotnet build UrlShortener.sln --no-restore` completed successfully; the build had zero warnings
  and zero errors. Temporary API/Redis processes, keys, and `task026-*` database records were
  removed. No automated test files were added.
