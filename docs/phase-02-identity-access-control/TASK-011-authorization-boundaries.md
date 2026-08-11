# TASK-011 — Authorization Boundaries and Access Enforcement

**Status:** Completed
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

- [x] Unauthenticated callers cannot access protected management endpoints.
- [x] User A cannot view private details/analytics of User B's protected link.
- [x] User A cannot activate, deactivate, edit, restore, or delete User B's link.
- [x] Server-side authorization does not depend on Angular hiding controls.
- [x] Ownership checks are applied before protected data is returned.
- [x] 401/403/404 behavior follows one documented policy.
- [x] Public redirect remains accessible without authentication.
- [x] Authorization failures use the common error contract and do not leak secrets.
- [x] Manual two-user verification is recorded.
- [x] No automated test files are added.

## Phase 02 Completion Gate

Phase 02 is complete when TASK-008 through TASK-011 are completed, authentication behavior is documented and manually verified, new managed URLs have immutable owners, and cross-user access is denied server-side.

## Implementation and Verification Record

Completed on 2026-08-11.

- Applied controller-level authorization to the complete `/api/v1/short-urls` management surface. The existing JWT challenge/forbidden handlers keep `401` and `403` responses in the common error envelope, while `/r/{shortCode}` remains on its anonymous controller.
- Replaced the global non-deleted management lookup with `GetOwnedByShortCodeNotDeletedAsync`. SQL Server and in-memory predicates combine the case-sensitive code, authenticated owner ID, and non-deleted state before returning a row. Application use cases also reject a missing current-user ID defensively.
- Documented the response policy in `docs/authorization.md`: unauthenticated calls use `401`; authenticated resource misses, deleted/legacy links, and non-owner access all use existence-concealing `404`; `403` is reserved for explicit non-resource role/scope policy failures. Future edit and restore endpoints must reuse the owner-scoped boundary; neither operation exists in the current Phase 02 API, so no callable cross-owner edit/restore surface is exposed.
- Registered User A `81949721-0607-4f3c-9ee8-5abf4acab1ae` with link `UxITyk` and User B `822dc032-371c-4d48-8406-8460e278a8bb` with link `kjEoLU` against Development LocalDB. Every management route returned `401 AUTHENTICATION_REQUIRED` without a token. User A received `404 NOT_FOUND` for User B's details, stats, status change, and deletion; the normalized details body exactly matched the unknown-code response. User B received `200` for details, stats, deactivate, and activate, then `204` for deletion.
- Confirmed unauthenticated `GET /r/UxITyk` returned `302` to `https://example.com/task011-a`. Generated OpenAPI contained Bearer requirements for all five management operations and none for public redirect.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. `dotnet format UrlShortener.sln --verify-no-changes --no-restore` completed successfully. No automated test files were added.
