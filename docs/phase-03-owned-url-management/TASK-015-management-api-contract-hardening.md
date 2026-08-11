# TASK-015 — Management API Contract Hardening

**Status:** Planned  
**Phase:** 03 — Owned URL Lifecycle & Management API

## Goal

Normalize the completed owned-link API into a stable contract that the Angular application can consume without duplicating backend rules.

## Dependencies

- TASK-014 completed.

## Scope

- Review create/list/detail/update/status/delete/restore contracts as one API surface.
- Normalize DTO naming, UTC timestamps, pagination metadata, validation errors, not-found/forbidden semantics, and short-URL construction.
- Ensure external base URL generation is configuration/proxy aware rather than accidentally tied to an internal container host.
- Remove accidental entity exposure from API responses.
- Produce concise API examples for Angular implementation.

## Acceptance Criteria

- [ ] All link-management endpoints use one consistent versioning and error strategy.
- [ ] Domain/EF entities are not serialized directly as public contracts.
- [ ] UTC timestamps are represented consistently.
- [ ] Pagination and filtering metadata are consistent across collection responses.
- [ ] Public short URL construction works correctly behind the documented reverse-proxy/base-URL model.
- [ ] 401/403/404 decisions agree with Phase 02 authorization policy.
- [ ] OpenAPI contains current request/response schemas and authentication requirements.
- [ ] A concise management API usage section is added to documentation for the Angular phase.
- [ ] Build and representative manual calls succeed.
- [ ] No automated test files are added.

## Phase 03 Completion Gate

Phase 03 is complete when TASK-012 through TASK-015 are completed and the backend exposes a stable, owner-scoped URL-management API suitable for direct Angular integration.