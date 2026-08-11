# TASK-010 — User Ownership Model for Managed Resources

**Status:** Planned  
**Phase:** 02 — Identity & Access Control

## Goal

Introduce explicit ownership so every protected short URL and future user-owned resource can be authorized server-side.

## Dependencies

- TASK-009 completed.

## Scope

- Add owner identity to `ShortUrl` or the approved equivalent.
- Define how existing pre-authentication rows are migrated: assigned, marked legacy/anonymous, or otherwise handled by an explicit migration rule.
- Introduce a current-user/access-context abstraction usable by application use cases without coupling core logic to controller internals.
- Define ownership rules for future API keys and custom domains.
- Preserve public redirect behavior: ownership must not be required to follow a valid public short URL.

## Acceptance Criteria

- [ ] Every newly created authenticated short URL stores an immutable owner relationship.
- [ ] Database foreign keys/indexes required for efficient owner-scoped queries exist.
- [ ] Legacy rows have a documented deterministic migration treatment.
- [ ] Application code can request current authenticated user identity through one clear abstraction.
- [ ] Redirect resolution remains public for valid links.
- [ ] Ownership identifiers are not accepted from client request bodies when they can be derived from authenticated context.
- [ ] Owner identity cannot be changed through normal URL update operations.
- [ ] Build and migration application succeed.
- [ ] No automated test files are added.

## Verification

Create at least two users and owned links manually. Inspect persistence to confirm ownership is derived from authentication context rather than caller-supplied identifiers.