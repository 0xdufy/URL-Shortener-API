# TASK-013 — Owned URL Listing, Search, Filter, Sort, and Pagination

**Status:** Completed
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

- [x] Collection results contain only resources owned by the authenticated user.
- [x] Page size has a documented default and maximum.
- [x] Invalid page/filter/sort values produce consistent validation errors.
- [x] Sorting is deterministic across pages.
- [x] Search/filtering executes server-side rather than loading all rows into memory.
- [x] Query projection returns only fields required by list views.
- [x] Relevant database indexes are added or their absence is explicitly justified.
- [x] Response includes sufficient pagination metadata for the Angular client.
- [x] OpenAPI documents query parameters and response shape.
- [x] Build succeeds and representative combinations are manually verified.
- [x] No automated test files are added.

## Verification

Create multiple links for two users, including inactive/expired examples, and verify pagination/filtering never crosses ownership boundaries.

## Implementation and Verification Record

Completed on 2026-08-11.

- Added authenticated `GET /api/v1/short-urls` with page 1/page size 20 defaults and a page-size maximum of 100. The response includes items, current page and size, total items and pages, and previous/next flags.
- Added server-side contains search, active-state and expiration filters, soft-deleted inclusion, inclusive UTC creation bounds, and four documented sort fields. Every ordering appends `Id` in the same direction for deterministic ties.
- SQL Server composes ownership and all optional predicates before `Count`, `OrderBy`, list-only projection, and `Skip`/`Take`. The Development in-memory repository mirrors the contract. Added migration `20260811194549_AddOwnedShortUrlQueryIndex` for `OwnerId, IsDeleted, CreatedAtUtc, Id`; the conventional-index limitations for arbitrary substring search and optional low-selectivity filters are documented in `docs/owned-url-query.md`.
- OpenAPI exposes all ten query parameters with their defaults/rules, the `ShortUrlListResponse` schema, and the Bearer requirement.
- Against a dedicated migrated LocalDB database, User A `991c0758-16ca-4866-a01a-e033d5ebc129` owned four links and User B `a42a1aee-8979-4cce-b90c-ccf3d6b20e25` owned one. User A's default query returned three non-deleted links without User B's row; deleted inclusion returned four with exactly one deleted row. User B's query returned only its own row. Inactive, expired, and combined active/not-expired/search queries each returned the intended link.
- Set all four User A creation timestamps equal and queried two pages of size two. Both pages reported two total pages and had no overlapping IDs, exercising the stable tie-breaker. An inclusive equal-bound creation range returned all four rows. Invalid page, page size, expiration, sort field, sort direction, and non-UTC offset values returned `400 VALIDATION_ERROR` with corresponding field details.
- `dotnet format UrlShortener.sln --no-restore` and `dotnet build UrlShortener.sln --no-restore` completed successfully with zero warnings and errors. No automated test files were added.
