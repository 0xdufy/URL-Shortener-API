# TASK-020 — Dashboard and Owned Link List UI

**Status:** Completed
**Phase:** 05 — Angular Auth, Dashboard & Link Management

## Goal

Provide the primary authenticated workspace for discovering, filtering, and acting on owned links.

## Dependencies

- TASK-019 completed.

## Scope

- Build dashboard summary cards using currently available backend data without inventing misleading analytics.
- Build the owned-links table/list backed by server pagination, search, filters, and sorting.
- Provide short URL and destination display, status/expiry indicators, creation timestamp, copy action, and navigation to link details.
- Synchronize useful query state with URL query parameters when practical.
- Implement loading, empty, filtered-empty, error, unauthorized, and rate-limited states.

## Acceptance Criteria

- [x] Link list uses server-side pagination/filter/sort from Phase 03.
- [x] No cross-user data can be inferred or displayed.
- [x] Copy-short-URL action provides explicit success/failure feedback.
- [x] Active/inactive/expired/deleted-related states are not represented ambiguously.
- [x] Search/filter controls are debounced or intentionally triggered to avoid excessive API calls.
- [x] Pagination remains valid when filters change.
- [x] Empty state offers a clear create-link action.
- [x] Long destination URLs do not break responsive layout and remain inspectable.
- [x] Desktop and mobile layouts remain usable.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Verification

Use a user with enough seeded/manual links to exercise multiple pages, filters, long URLs, expired and inactive states, and an empty account.

## Completion Notes

- Replaced the dashboard foundation sample and Links placeholder with one owner-scoped list page
  backed exclusively by the Phase 03 list endpoint. Server pagination, search, activity/expiry/deleted
  filters, four sort fields, and sort direction are synchronized with compact URL query parameters.
- Added debounced search, intentional select requests, in-flight request cancellation, filter-driven
  page resets, and stale-page correction so rapid changes and shrinking result sets remain valid.
- Added honest summary cards limited to total matching rows plus click and active counts for the
  currently loaded page; the UI does not infer unavailable workspace-wide analytics.
- Added explicit Deleted, Expired, Inactive, and Active precedence; canonical short URL copy with
  success/failure toasts; inspectable ellipsized destinations; creation/expiry context; and detail
  navigation placeholders owned by TASK-022.
- Added initial loading, refreshing, empty, filtered-empty, connectivity/service, authorization,
  unauthorized, and retry-aware rate-limit states with responsive desktop table and mobile cards.
- Browser-checked on 2026-08-12 at desktop size and 390×844 using an authenticated empty account:
  protected routing, initial empty state, debounced URL-synchronized search, filtered-empty state,
  accessible labels, and zero horizontal overflow all behaved as intended.
- Verified Prettier, Angular ESLint, and the production Angular build. The page's responsive component
  CSS emits the configured 4 kB warning but remains below the 8 kB error budget. No automated test
  files were added; multi-page and lifecycle-rich manual fixtures remain useful follow-up coverage.
