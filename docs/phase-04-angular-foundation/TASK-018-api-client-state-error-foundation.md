# TASK-018 — Typed API Client, Auth State Boundary, and Error Handling Foundation

**Status:** Planned  
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

- [ ] Feature components are not expected to construct raw API URLs manually.
- [ ] API request/response types align with Phase 03 contracts.
- [ ] Authentication credentials are transported according to the Phase 02 ADR.
- [ ] 401 behavior cannot create redirect loops.
- [ ] 429 responses can surface retry guidance when supplied by the API.
- [ ] Validation errors can be mapped to forms without parsing arbitrary strings.
- [ ] Unexpected errors retain a correlation/trace identifier when the backend exposes one.
- [ ] Error handling distinguishes user-actionable failures from generic service failures.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Phase 04 Completion Gate

Phase 04 is complete when TASK-016 through TASK-018 are completed and the Angular application has a reproducible build, reusable shell/design system, and one typed API/error/auth-state foundation ready for feature implementation.