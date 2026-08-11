# TASK-037 — API Key Security Model and Persistence

**Status:** Planned  
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

- [ ] Plaintext API-key secret is never stored in the database or logs.
- [ ] Key secret uses cryptographically secure randomness with documented entropy.
- [ ] Database stores enough non-secret metadata to list/manage keys safely.
- [ ] User can revoke a key without deleting audit-relevant metadata immediately.
- [ ] Expired/revoked keys are distinguishable internally and rejected by future auth middleware.
- [ ] Scope representation is explicit and not an arbitrary unchecked string bag.
- [ ] Creation response is the only normal path that reveals the full secret.
- [ ] Migrations apply cleanly and indexes support credential lookup/user listing.
- [ ] Backend build succeeds.
- [ ] No automated test files are added.

## Verification

Create a key, inspect persistence/logs to confirm plaintext absence, and verify its hash/lookup metadata and revocation/expiry state transitions manually.