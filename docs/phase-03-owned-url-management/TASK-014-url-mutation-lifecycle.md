# TASK-014 — URL Update, Status, Delete, and Restore Lifecycle

**Status:** Planned  
**Phase:** 03 — Owned URL Lifecycle & Management API

## Goal

Provide a coherent lifecycle for owned links so management behavior is predictable for both API clients and the Angular UI.

## Dependencies

- TASK-013 completed.

## Scope

- Update supported mutable fields such as destination URL and expiry according to an explicit contract.
- Activate/deactivate owned links.
- Soft delete owned links.
- Restore soft-deleted owned links when retention rules permit.
- Decide whether aliases are immutable; if mutable, define conflict and cache-invalidation semantics explicitly.
- Ensure every redirect-affecting mutation invalidates relevant cache entries through an abstraction compatible with the later Redis phase.
- Define concurrency behavior for conflicting edits; add optimistic concurrency only if justified and documented.

## Acceptance Criteria

- [ ] Only the owner can mutate the resource.
- [ ] Owner identity and immutable system fields cannot be changed by request payloads.
- [ ] Destination/expiry updates are validated with the same rules as creation.
- [ ] Deactivated links no longer redirect according to the documented status behavior.
- [ ] Soft-deleted links are excluded from normal owner lists and redirect resolution.
- [ ] Restore behavior is explicit and cannot restore beyond a future hard-delete boundary.
- [ ] Redirect-affecting mutations invalidate cached redirect state.
- [ ] Alias mutability is explicitly documented; conflicts are handled predictably if changes are allowed.
- [ ] API response/status codes are documented in OpenAPI.
- [ ] Build and manual lifecycle verification succeed.
- [ ] No automated test files are added.

## Verification

Exercise create → edit → deactivate → activate → soft delete → restore with two users and verify ownership, redirect behavior, and cache invalidation semantics.