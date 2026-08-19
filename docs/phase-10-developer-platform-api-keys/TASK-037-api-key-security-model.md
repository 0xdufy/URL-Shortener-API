# TASK-037 — API Key Security Model and Persistence

**Status:** Completed
**Phase:** 10 — Developer Platform & API Keys

## Goal

Add secure user-owned API-key credentials for programmatic access without storing recoverable plaintext secrets.

## Dependencies

- Phase 09 completed.

## Scope

- Define key format with a non-secret lookup identifier/prefix and high-entropy secret portion.
- Store only an approved one-way hash/verification representation of the secret.
- Support user-provided key name, scopes, creation time, optional expiry, last-used metadata, revoked state, and rotation/replacement semantics.
- Show plaintext key only once at creation/rotation.
- Define maximum active keys per user and naming constraints.

## Acceptance Criteria

- [x] Plaintext API-key secret is never stored in the database or logs.
- [x] Key secret uses cryptographically secure randomness with documented entropy.
- [x] Database stores enough non-secret metadata to list/manage keys safely.
- [x] User can revoke a key without deleting audit-relevant metadata immediately.
- [x] Expired/revoked keys are distinguishable internally and rejected by future auth middleware.
- [x] Scope representation is explicit and not an arbitrary unchecked string bag.
- [x] Creation response is the only normal path that reveals the full secret.
- [x] Migrations apply cleanly and indexes support credential lookup/user listing.
- [x] Backend build succeeds.
- [x] No automated test files are added.

## Verification

Create a key, inspect persistence/logs to confirm plaintext absence, and verify its hash/lookup metadata and revocation/expiry state transitions manually.

## Implementation and Verification Notes

- 2026-08-19: Added the `usk_<128-bit-public-lookup>.<256-bit-secret>` credential format using
  `RandomNumberGenerator`, unpadded base64url, and SHA-256 over the decoded uniformly random secret.
  Only the binary digest and public prefix are persisted; create and rotate return the assembled
  credential once with `Cache-Control: no-store`.
- Added owner-scoped create/list/revoke/rotate application services and `/api/v1/api-keys`
  management routes. Names have bounded ASCII constraints, the active-key limit is fixed at 10,
  and scopes are a four-value flags enum with request allowlisting plus a database bitmask check.
- Revocation retains metadata. Rotation runs in a serializable transaction, inserts a replacement
  with the same name/scopes/expiry, revokes the predecessor, and records the replacement ID.
  Metadata lists derive distinct `active`, `expired`, and `revoked` states.
- Migration `20260819134004_AddApiKeySecurityModel` was applied with the full migration chain to a
  new isolated LocalDB database. EF reported no pending model changes afterward.
- A temporary, uncommitted verification harness confirmed the persisted digest matches the decoded
  secret while neither plaintext form is present in persisted strings, enforced the active-key
  cap, exercised atomic rotation/replacement, and verified retained revocation state. No `usk_`
  credential appeared in repository logs. The harness and isolated database were removed after use.
- Targeted `dotnet format --verify-no-changes --no-restore`, `git diff --check`, and
  `dotnet build UrlShortener.sln --no-restore` succeeded; the backend build completed with zero
  warnings and zero errors. The repository-wide formatter still reports pre-existing findings in
  older analytics source/migration files outside TASK-037. No automated test file was added.
