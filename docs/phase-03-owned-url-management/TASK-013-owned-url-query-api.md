# TASK-013 — Owned URL Listing, Search, Filter, Sort, and Pagination

**Status:** Planned  
**Phase:** 03 — Owned URL Lifecycle & Management API

## Goal

Provide an efficient owner-scoped query API that can power the Angular dashboard without exposing other users' data or requiring client-side filtering.

## Dependencies

- TASK-012 completed.

## Scope

Add an owner-scoped collection endpoint supporting bounded pagination plus documented search/filter/sort options. At minimum consider:

- Search by short code/custom alias and destination URL.
- Active/inactive state.
- Expired/not expired state.
- Deleted/non-deleted behavior according to the product contract.
- Created-date range.
- Deterministic sorting, including a stable tie-breaker.

Review EF Core query shape and indexes against the selected filters/orderings.

## Acceptance Criteria

- [ ] Collection results contain only resources owned by the authenticated user.
- [ ] Page size has a documented default and maximum.
- [ ] Invalid page/filter/sort values produce consistent validation errors.
- [ ] Sorting is deterministic across pages.
- [ ] Search/filtering executes server-side rather than loading all rows into memory.
- [ ] Query projection returns only fields required by list views.
- [ ] Relevant database indexes are added or their absence is explicitly justified.
- [ ] Response includes sufficient pagination metadata for the Angular client.
- [ ] OpenAPI documents query parameters and response shape.
- [ ] Build succeeds and representative combinations are manually verified.
- [ ] No automated test files are added.

## Verification

Create multiple links for two users, including inactive/expired examples, and verify pagination/filtering never crosses ownership boundaries.