# TASK-020 — Dashboard and Owned Link List UI

**Status:** Planned  
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

- [ ] Link list uses server-side pagination/filter/sort from Phase 03.
- [ ] No cross-user data can be inferred or displayed.
- [ ] Copy-short-URL action provides explicit success/failure feedback.
- [ ] Active/inactive/expired/deleted-related states are not represented ambiguously.
- [ ] Search/filter controls are debounced or intentionally triggered to avoid excessive API calls.
- [ ] Pagination remains valid when filters change.
- [ ] Empty state offers a clear create-link action.
- [ ] Long destination URLs do not break responsive layout and remain inspectable.
- [ ] Desktop and mobile layouts remain usable.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Verification

Use a user with enough seeded/manual links to exercise multiple pages, filters, long URLs, expired and inactive states, and an empty account.