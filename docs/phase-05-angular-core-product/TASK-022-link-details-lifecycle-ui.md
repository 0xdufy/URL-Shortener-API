# TASK-022 — Link Details and Lifecycle Actions UI

**Status:** Completed
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

- [x] Details page loads only authenticated user's accessible resources.
- [x] Short URL and destination are visually distinguishable.
- [x] Activation state changes are reflected immediately after successful API response.
- [x] Delete requires explicit confirmation and redirects or transitions to an appropriate deleted state.
- [x] Restore is shown only when the API contract permits it.
- [x] 404/authorization/session failures render recoverable application states.
- [x] Copy/open actions provide feedback and do not create malformed URLs.
- [x] Mutations use the shared API/error layer rather than raw component HTTP calls.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Verification

Manually exercise active, inactive, expired, missing, deleted, restorable, and restore-window-expired
states; verify destructive confirmation, canonical URL copy/open behavior, and session recovery.

## Completion Notes

- Replaced the link-details placeholder with an authenticated owner-scoped page showing the
  canonical short URL, destination, click total, last access, timestamps, expiry, configured state,
  effective redirect state, and safe resource metadata. Canonical API URLs are rendered directly
  and validated as absolute HTTP/HTTPS values before copy or open actions are enabled.
- Added immediate activate/deactivate updates, explicit native-dialog deletion confirmation,
  soft-deleted state, and contract-aware restoration through `ShortUrlsApiClient`. Deleted details
  are recovered through an exact-code match in the owner-scoped `includeDeleted` collection because
  the detail endpoint intentionally conceals deleted resources with `404`; pagination prevents a
  valid deleted match from being missed in a large filtered result set.
- Added distinct active, inactive, expired, and deleted guidance. Restore is offered only while the
  server-provided exclusive deadline is in the future; stale `404`, restore conflict, and expired
  restore-window responses trigger recovery paths instead of leaving controls in an unusable state.
- Added actionable loading, missing/unauthorized, expired-session, connectivity, service,
  rate-limit, and mutation error states. All reads and mutations use the shared typed API client and
  normalized `ApiError` layer.
- Registered `/app/links/:shortCode/analytics` ahead of the details route as the Phase 09 navigation
  placeholder, so per-link analytics navigation does not fall through to the details component.
- Browser-checked on 2026-08-12 with an isolated authenticated local account: creation-to-details,
  canonical clipboard value, deactivate/activate, analytics navigation, delete cancellation,
  confirmed deletion, deleted-state recovery after a fresh authenticated navigation, restore, and a
  concealed missing-link state all behaved as intended. A 390×844 viewport had no horizontal
  overflow, and the browser console remained free of errors.
- Prettier, Angular ESLint, and the production Angular build completed successfully. The new detail
  stylesheet emits the configured 4 kB component-style warning at 7.20 kB but remains below the 8 kB
  error budget. No automated test files were added.

## Phase 05 Completion Gate

Phase 05 is complete when TASK-019 through TASK-022 are completed and a user can authenticate and perform the full supported owned-link lifecycle through Angular without using Swagger for normal product workflows.
