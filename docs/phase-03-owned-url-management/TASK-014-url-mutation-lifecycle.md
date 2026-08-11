# TASK-014 — URL Update, Status, Delete, and Restore Lifecycle

**Status:** Completed
**Phase:** 03 — Owned URL Lifecycle & Management API

## Goal

Provide a coherent lifecycle for owned links so management behavior is predictable for both API clients and the Angular UI.

## Dependencies

- TASK-013 completed.

## Scope

- Update supported mutable fields such as destination URL and expiry according to an explicit contract.
- Activate/deactivate owned links.
- Soft delete owned links.
- Restore soft-deleted owned links when retention rules permit.
- Decide whether aliases are immutable; if mutable, define conflict and cache-invalidation semantics explicitly.
- Ensure every redirect-affecting mutation invalidates relevant cache entries through an abstraction compatible with the later Redis phase.
- Define concurrency behavior for conflicting edits; add optimistic concurrency only if justified and documented.

## Acceptance Criteria

- [x] Only the owner can mutate the resource.
- [x] Owner identity and immutable system fields cannot be changed by request payloads.
- [x] Destination/expiry updates are validated with the same rules as creation.
- [x] Deactivated links no longer redirect according to the documented status behavior.
- [x] Soft-deleted links are excluded from normal owner lists and redirect resolution.
- [x] Restore behavior is explicit and cannot restore beyond a future hard-delete boundary.
- [x] Redirect-affecting mutations invalidate cached redirect state.
- [x] Alias mutability is explicitly documented; conflicts are handled predictably if changes are allowed.
- [x] API response/status codes are documented in OpenAPI.
- [x] Build and manual lifecycle verification succeed.
- [x] No automated test files are added.

## Verification

Exercise create → edit → deactivate → activate → soft delete → restore with two users and verify ownership, redirect behavior, and cache invalidation semantics.

## Implementation and Verification Record

Completed on 2026-08-11.

- Added owner-scoped full-replacement `PUT /api/v1/short-urls/{shortCode}` for destination and expiry. The update validator mirrors creation's absolute HTTP/HTTPS, 2,048-character, future-UTC expiry rules; a null/omitted expiry clears it. The request has no owner, alias, ID, creation, deletion, status, or counter fields.
- Kept short codes/aliases immutable and globally claimed through soft deletion. Added explicit status validation so omitting `isActive` returns `400` instead of silently binding to `false`.
- Completed soft-delete/restore behavior with configurable `ShortUrlLifecycle:SoftDeleteRetentionDays` (30-day default, 1–3,650 validation). Restore preserves active state, rejects non-deleted rows with `409 RESTORE_NOT_DELETED`, and rejects missing/expired deletion timestamps with `410 RESTORE_WINDOW_EXPIRED` at the exclusive hard-delete eligibility boundary.
- Destination/expiry, status, deletion, and restore commits all invalidate `IShortUrlCache`. Added `deletedAtUtc` and `restoreUntilUtc` to deleted list projections and lifecycle details.
- Documented alias immutability, owner concealment, redirect results, retention, cache behavior, last-commit-wins concurrency, configuration, and endpoint status codes in `docs/url-lifecycle.md`, README, and generated OpenAPI operation metadata.
- Against dedicated migrated LocalDB database `UrlShortenerTask014Verification`, two distinct authenticated users exercised create → edit → deactivate → activate → soft delete → restore. Immediate redirects changed from the original destination to the edited destination, then `404`, `302`, `404`, and `302`, demonstrating invalidation after each relevant mutation. The other owner received `404 NOT_FOUND` for update and restore.
- Default owner listing returned zero rows after deletion while `includeDeleted=true` returned one row with deletion time. Repeated restore returned `409`; a deletion timestamp set 31 days old returned `410`. Supplied owner/ID/alias fields did not change system fields, and invalid URL/past-expiry edits returned `400 VALIDATION_ERROR`.
- OpenAPI exposed lifecycle summaries and response sets: update `200/400/401/404`, status `200/400/401/404`, delete `204/401/404`, and restore `200/401/404/409/410`.
- `dotnet format UrlShortener.sln --verify-no-changes --no-restore` and `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. No automated test files were added.
