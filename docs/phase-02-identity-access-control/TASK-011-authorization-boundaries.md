# TASK-011 — Authorization Boundaries and Access Enforcement

**Status:** Planned  
**Phase:** 02 — Identity & Access Control

## Goal

Enforce server-side authorization consistently so protected resources cannot be read or mutated across user boundaries.

## Dependencies

- TASK-010 completed.

## Scope

- Require authentication for URL-management endpoints that expose private data or mutation capabilities.
- Scope lookups by authenticated owner rather than fetching globally then relying on UI behavior.
- Define approved 401/403/404 semantics, including whether resource existence is concealed from non-owners.
- Centralize reusable ownership/authorization policy where appropriate.
- Ensure Swagger/OpenAPI reflects protected endpoints.

## Acceptance Criteria

- [ ] Unauthenticated callers cannot access protected management endpoints.
- [ ] User A cannot view private details/analytics of User B's protected link.
- [ ] User A cannot activate, deactivate, edit, restore, or delete User B's link.
- [ ] Server-side authorization does not depend on Angular hiding controls.
- [ ] Ownership checks are applied before protected data is returned.
- [ ] 401/403/404 behavior follows one documented policy.
- [ ] Public redirect remains accessible without authentication.
- [ ] Authorization failures use the common error contract and do not leak secrets.
- [ ] Manual two-user verification is recorded.
- [ ] No automated test files are added.

## Phase 02 Completion Gate

Phase 02 is complete when TASK-008 through TASK-011 are completed, authentication behavior is documented and manually verified, new managed URLs have immutable owners, and cross-user access is denied server-side.