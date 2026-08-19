# TASK-038 — API Key Authentication, Scopes, and Rate Limits

**Status:** Completed
**Phase:** 10 — Developer Platform & API Keys

## Goal

Authenticate API-key requests efficiently and authorize each request by both owning user and explicit scope.

## Dependencies

- TASK-037 completed.

## Scope

- Add API-key authentication handler/middleware through an explicit credential scheme.
- Verify hashed secret using timing-safe/provider-appropriate comparison.
- Resolve owning user/account status.
- Enforce scopes such as approved equivalents of `shorturls:create`, `shorturls:read`, `shorturls:write`, and `analytics:read`.
- Integrate API-key identity with Phase 07 distributed rate limiting.
- Update last-used metadata without turning every request into an avoidable synchronous write if a lower-cost bounded strategy is appropriate.

## Acceptance Criteria

- [x] Invalid, expired, revoked, or owner-disabled keys are rejected.
- [x] API-key authentication does not expose whether a guessed lookup prefix exists beyond the approved error policy.
- [x] Requests cannot exceed the scopes assigned to the key.
- [x] Resource ownership still applies after scope authorization; a key cannot access another user's links.
- [x] API-key rate-limit identity is distinct from normal browser session identity where policy requires it.
- [x] Authentication credentials never appear in logs or exception bodies.
- [x] Key lookup uses an indexed non-secret identifier and secure secret verification.
- [x] OpenAPI describes API-key authentication and scope requirements where supported.
- [x] Manual valid/invalid/revoked/insufficient-scope verification succeeds.
- [x] No automated test files are added.

## Verification

Exercise one key per scope combination, a revoked key, expired key, invalid secret, and an attempted cross-owner resource access.

## Implementation and Verification Notes

- 2026-08-19: Added the explicit `Authorization: ApiKey <credential>` authentication scheme behind
  a Bearer-or-API-key selector. The SQL lookup joins the indexed public prefix to its owner status,
  hashes the decoded random secret, and uses `CryptographicOperations.FixedTimeEquals`, including a
  fixed-length dummy digest for unknown prefixes. Invalid, expired, revoked, suspended/disabled-owner,
  and wrong-secret credentials share the same safe `401` response.
- Added four scope policies. Short-URL create/read/write and analytics endpoints accept either JWT
  Bearer identity or an API key with the exact required scope; API-key management and account/session
  operations remain JWT-only. Successful key identities carry the immutable owner in `sub`, so the
  existing owner-scoped persistence boundary continues to conceal cross-owner resources with `404`.
- API-key calls now override endpoint session limits with the distributed `ApiKey` token bucket,
  partitioned by `api_key_id`. `LastUsedAtUtc` uses the lookup result plus a conditional five-minute
  write interval, avoiding a write on every request and suppressing concurrent duplicate updates.
- OpenAPI now defines the API-key authorization-header scheme as an alternative only on scoped
  operations and appends the required scope to each operation description. The API-key and
  rate-limiting documentation describes the request, scope, ownership, error, and partition contracts.
- An isolated fully migrated LocalDB database and running Redis instance were used for manual HTTP
  verification. Individual create, read, write, and analytics keys and a combined key succeeded on
  permitted operations. Insufficient scope returned `403`; wrong-secret, unknown-prefix, revoked,
  expired, and disabled-owner keys returned the same `401`; API-key management rejected an API key;
  and an owner-A read key received concealed `404` for owner B's link. Two immediate valid requests
  retained the first `LastUsedAtUtc` value. Application logs contained no `usk_` credential text.
  The isolated database was removed afterward.
- `dotnet build UrlShortener.sln --no-restore`, targeted formatting verification, and
  `git diff --check` succeeded. No automated test file was added.
