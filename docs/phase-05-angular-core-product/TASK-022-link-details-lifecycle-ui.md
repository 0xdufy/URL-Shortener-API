# TASK-022 — Link Details and Lifecycle Actions UI

**Status:** Planned  
**Phase:** 05 — Angular Auth, Dashboard & Link Management

## Goal

Provide one authoritative link-details page for inspection and lifecycle actions before advanced analytics is added.

## Dependencies

- TASK-021 completed.

## Scope

- Build link detail view with destination, short URL, timestamps, expiry, active state, click-count/basic stats currently available, and ownership-safe metadata.
- Add activate/deactivate, edit, soft-delete, and restore actions according to Phase 03 behavior.
- Use confirmation for destructive actions.
- Handle stale/not-found/deleted/expired states without leaving unusable UI.
- Provide navigation placeholder to the future analytics page.

## Acceptance Criteria

- [ ] Details page loads only authenticated user's accessible resources.
- [ ] Short URL and destination are visually distinguishable.
- [ ] Activation state changes are reflected immediately after successful API response.
- [ ] Delete requires explicit confirmation and redirects or transitions to an appropriate deleted state.
- [ ] Restore is shown only when the API contract permits it.
- [ ] 404/authorization/session failures render recoverable application states.
- [ ] Copy/open actions provide feedback and do not create malformed URLs.
- [ ] Mutations use the shared API/error layer rather than raw component HTTP calls.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Phase 05 Completion Gate

Phase 05 is complete when TASK-019 through TASK-022 are completed and a user can authenticate and perform the full supported owned-link lifecycle through Angular without using Swagger for normal product workflows.