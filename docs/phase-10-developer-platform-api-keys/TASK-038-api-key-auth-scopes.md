# TASK-038 — API Key Authentication, Scopes, and Rate Limits

**Status:** Planned  
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

- [ ] Invalid, expired, revoked, or owner-disabled keys are rejected.
- [ ] API-key authentication does not expose whether a guessed lookup prefix exists beyond the approved error policy.
- [ ] Requests cannot exceed the scopes assigned to the key.
- [ ] Resource ownership still applies after scope authorization; a key cannot access another user's links.
- [ ] API-key rate-limit identity is distinct from normal browser session identity where policy requires it.
- [ ] Authentication credentials never appear in logs or exception bodies.
- [ ] Key lookup uses an indexed non-secret identifier and secure secret verification.
- [ ] OpenAPI describes API-key authentication and scope requirements where supported.
- [ ] Manual valid/invalid/revoked/insufficient-scope verification succeeds.
- [ ] No automated test files are added.

## Verification

Exercise one key per scope combination, a revoked key, expired key, invalid secret, and an attempted cross-owner resource access.