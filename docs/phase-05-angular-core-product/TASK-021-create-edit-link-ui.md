# TASK-021 — Create and Edit Link UI

**Status:** Completed
**Phase:** 05 — Angular Auth, Dashboard & Link Management

## Goal

Deliver complete Angular forms for creating and editing owned short links using backend validation as the authoritative contract.

## Dependencies

- TASK-020 completed.

## Scope

- Build create-link workflow for destination URL, optional custom alias, and optional expiry.
- Build edit workflow for fields declared mutable by Phase 03.
- Provide clear generated-vs-custom alias behavior.
- Map backend field-validation and alias-conflict responses to actionable form feedback.
- Show created short URL with copy/navigation actions after success.
- Prevent accidental duplicate submissions while a request is in flight.

## Acceptance Criteria

- [x] Create form supports all Phase 03 creation fields and no unauthorized/system fields.
- [x] Edit form exposes only fields the API declares mutable.
- [x] Invalid URL, alias, expiry, conflict, rate-limit, and unexpected errors have distinct understandable feedback.
- [x] Client-side validation improves UX but does not replace server validation.
- [x] Submit controls show pending state and prevent accidental repeat submission.
- [x] Successful create/edit updates relevant cached/view state without requiring a full browser reload.
- [x] Created short URL can be copied and opened.
- [x] Form remains keyboard accessible and responsive.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Verification

Manually exercise generated code, custom alias, duplicate alias, invalid destination, invalid expiry, successful edit, and server-side validation mismatch scenarios.

## Completion Notes

- Added a shared route-aware create/edit page backed exclusively by `ShortUrlsApiClient`. Creation
  exposes destination, explicit generated/custom alias selection, and optional local-time expiry;
  editing loads the owned resource and exposes only the Phase 03 mutable destination and expiry
  fields while keeping the permanent short code visible as read-only context.
- Added client-side absolute HTTP/HTTPS, length, alias-character, required-custom-alias, and future
  expiry guidance. API validation details are normalized back to their fields and remain
  authoritative; alias conflicts, rate limits with retry timing, connectivity, service, not-found,
  and unexpected failures have distinct persistent feedback without clearing form values.
- Disabled the full form and submit action while each mutation is pending. Successful creation
  transitions in place to a share-ready result with copy, open, details, and create-another actions;
  successful editing replaces the local resource/form state and confirms the change without a
  browser reload.
- Added owner-list edit entry points for current links and registered the dedicated
  `/app/links/:shortCode/edit` route ahead of the details route. Deleted links intentionally omit
  the edit action because the API does not expose deleted resources through the detail/update
  endpoints.
- Browser-checked on 2026-08-12 with an authenticated in-memory account: invalid destination and
  missing custom alias focus their actionable fields; generated creation, custom creation with
  expiry, duplicate alias (`409 ALIAS_CONFLICT`), successful destination edit/expiry clearing,
  clipboard copy, and list-to-edit navigation all behaved as intended. A 390×844 viewport had no
  horizontal overflow, and the browser console remained free of errors.
- Prettier, Angular ESLint, and the production Angular build completed successfully. The new form
  stylesheet emits the configured 4 kB component-style warning at 7.71 kB but remains below the
  8 kB error budget. No automated test files were added.
