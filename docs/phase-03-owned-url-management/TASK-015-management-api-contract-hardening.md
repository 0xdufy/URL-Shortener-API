# TASK-015 — Management API Contract Hardening

**Status:** Completed
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

- [x] All link-management endpoints use one consistent versioning and error strategy.
- [x] Domain/EF entities are not serialized directly as public contracts.
- [x] UTC timestamps are represented consistently.
- [x] Pagination and filtering metadata are consistent across collection responses.
- [x] Public short URL construction works correctly behind the documented reverse-proxy/base-URL model.
- [x] 401/403/404 decisions agree with Phase 02 authorization policy.
- [x] OpenAPI contains current request/response schemas and authentication requirements.
- [x] A concise management API usage section is added to documentation for the Angular phase.
- [x] Build and representative manual calls succeed.
- [x] No automated test files are added.

## Phase 03 Completion Gate

Phase 03 is complete when TASK-012 through TASK-015 are completed and the backend exposes a stable, owner-scoped URL-management API suitable for direct Angular integration.

## Implementation and Verification Record

Completed on 2026-08-12.

- Kept the complete management surface under `/api/v1/short-urls` and consolidated create/detail/update/status/restore onto one `ShortUrlResponse` public schema. List results retain a projection-only item schema. No controller serializes a domain or EF entity.
- Added canonical `shortUrl` to both resource and list contracts. `PublicUrls:BaseUrl` is required and startup-validated as a root HTTP/HTTPS origin; deployed instances use `PublicUrls__BaseUrl`. Public links no longer depend on the incoming request or an internal proxy/container host.
- Replaced flat collection paging fields with explicit `pagination` and normalized applied `filters` metadata. SQL-backed manual verification confirmed page metadata and normalization of case-insensitive enum inputs to `notExpired` and `createdAt`.
- Normalized SQL-loaded response timestamps to UTC before serialization. Manual create/delete/list verification confirmed `createdAtUtc`, `deletedAtUtc`, and `restoreUntilUtc` use the `Z` designator.
- Centralized error creation for controllers, middleware, JWT challenge/forbidden handlers, automatic model binding, and framework status results. Malformed JSON, unsupported media type, unknown route, disallowed method, missing authentication, validation, and application failures now share the `{ traceId, error: { code, message, details } }` envelope.
- Preserved the Phase 02 policy: missing/invalid identity returns `401`; missing, deleted, unowned, and cross-owner resources return existence-concealing `404`; `403` remains reserved for non-resource policy denial. A two-user SQL-backed call returned `404 NOT_FOUND` for cross-owner detail access.
- OpenAPI verification showed Bearer security and `401`/`403` responses on protected operations, list responses `200/400/401/403`, the new pagination/filter schemas, and `ShortUrlResponse` as the creation/lifecycle schema with no obsolete details schema.
- Added `docs/management-api.md` as the Angular handoff, with endpoint/status matrix, shared resource and collection examples, error handling, UTC rules, and the reverse-proxy/public-origin model. Updated the existing README, list, and lifecycle documentation to match.
- A disposable LocalDB database exercised register-two-users, create, list, update, deactivate, cross-owner read, delete, deleted listing, and restore. `Host: internal-api:8080` still returned `https://short.task015.example/r/Task015A`; the database was dropped after verification.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. `dotnet format UrlShortener.sln --verify-no-changes --no-restore` completed successfully. No automated test files were added.
