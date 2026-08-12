# TASK-018 — Typed API Client, Auth State Boundary, and Error Handling Foundation

**Status:** Completed
**Phase:** 04 — Angular Foundation & Design System

## Goal

Create one maintainable Angular boundary for backend communication, request correlation, authentication state integration, and standardized error handling before feature pages start calling HTTP directly.

## Dependencies

- TASK-017 completed.

## Scope

- Define typed models/client methods from the approved API contracts.
- Decide whether the client is generated from OpenAPI or maintained manually; document the trade-off and regeneration/update workflow.
- Centralize API base URL and HTTP configuration.
- Implement interceptors or equivalent boundaries for approved authentication transport, correlation headers if applicable, and standardized error translation.
- Define app-level handling for 401, 403, 404, 409, 410, 429, validation errors, connectivity failures, and unexpected 5xx responses.
- Establish an authentication-state service/store boundary without implementing all auth screens yet.

## Acceptance Criteria

- [x] Feature components are not expected to construct raw API URLs manually.
- [x] API request/response types align with Phase 03 contracts.
- [x] Authentication credentials are transported according to the Phase 02 ADR.
- [x] 401 behavior cannot create redirect loops.
- [x] 429 responses can surface retry guidance when supplied by the API.
- [x] Validation errors can be mapped to forms without parsing arbitrary strings.
- [x] Unexpected errors retain a correlation/trace identifier when the backend exposes one.
- [x] Error handling distinguishes user-actionable failures from generic service failures.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Verification

Run from `web/`:

```powershell
npm run format:check
npm run lint
npm run build
```

## Completion Notes

- Added manually maintained typed clients and models for Phase 02 authentication plus the complete
  Phase 03 owned-short-URL management surface. The manual-versus-generated decision and update
  workflow are documented in `web/README.md`.
- Registered centralized HTTP interceptors that scope Bearer credentials to the configured API,
  transport refresh cookies and CSRF headers only for approved auth operations, and attach a client
  request identifier without exposing credentials to unrelated URLs.
- Added an in-memory authentication-state boundary. Unauthorized transitions are idempotent and do
  not navigate or retry, leaving guards and safe return-URL behavior to TASK-019 without creating
  interceptor redirect loops.
- Normalized HTTP failures into `ApiError` categories covering 401, 403, 404, 409, 410, 429,
  validation, connectivity, and service/unexpected failures. Field details map directly by contract
  name, `Retry-After` becomes seconds, and server `traceId` plus client request ID remain available
  for support.
- Verified on 2026-08-12: Prettier check, Angular ESLint, and the production Angular build succeeded.
  No automated test files were added.

## Phase 04 Completion Gate

Phase 04 is complete when TASK-016 through TASK-018 are completed and the Angular application has a reproducible build, reusable shell/design system, and one typed API/error/auth-state foundation ready for feature implementation.
