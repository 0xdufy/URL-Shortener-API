# TASK-012 — Concurrency-Safe Owned URL Creation

**Status:** Planned  
**Phase:** 03 — Owned URL Lifecycle & Management API

## Goal

Make short-link creation correct under concurrent requests while preserving custom aliases, expiry, ownership, validation, and deterministic error behavior.

## Dependencies

- Phase 02 completed.

## Scope

- Refactor generated-code creation so database uniqueness is authoritative.
- Handle generated-code collisions through bounded retry after insert/unique-conflict detection rather than relying only on check-then-insert.
- Map custom-alias uniqueness conflicts to the documented API conflict response.
- Derive owner from authenticated context.
- Validate destination URL, alias, and expiry through application/API validation boundaries.
- Review short-code length/charset and document the chosen entropy/collision strategy.

## Acceptance Criteria

- [ ] Database unique constraint remains the final authority for short-code uniqueness.
- [ ] Generated short-code collisions are retried with a bounded attempt policy.
- [ ] Custom-alias conflicts return the documented conflict response and are not retried as generated-code collisions.
- [ ] Ownership is derived from the authenticated user, not request input.
- [ ] Invalid schemes and malformed/oversized URLs are rejected consistently.
- [ ] Expiry must follow the documented UTC/future-date policy.
- [ ] Failed creation does not leave partial records.
- [ ] OpenAPI reflects the current creation contract.
- [ ] Build succeeds and collision behavior is manually exercised where practical.
- [ ] Automated test files remain deferred to Phase 16.

## Verification

Document normal creation, custom alias creation, duplicate alias, and a reproducible/manual collision-path verification method.