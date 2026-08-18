# TASK-034 — Owner-Scoped Analytics Query API

**Status:** Completed
**Phase:** 09 — Advanced Analytics & Analytics UI

## Goal

Expose efficient, authorization-safe analytics endpoints that provide stable data for charts and summaries without leaking raw event details unnecessarily.

## Dependencies

- TASK-033 completed.

## Scope

- Define analytics summary and time-series endpoints for an owned link.
- Support bounded `from`/`to` ranges and approved granularity.
- Return total clicks, trend buckets, top referrers/sources, device/browser/OS breakdowns, and unique-visitor estimates where implemented.
- Define empty-data and partial/freshness semantics.
- Add limits to prevent abusive date-range/query cardinality.

## Acceptance Criteria

- [x] Only the link owner can access protected analytics.
- [x] Date ranges are bounded and validated.
- [x] Time buckets use documented UTC boundaries.
- [x] API exposes analytics freshness/eventual-consistency metadata when useful.
- [x] Routine queries use aggregate/indexed data rather than unbounded raw scans.
- [x] Empty analytics returns a valid empty model rather than an avoidable server error.
- [x] Unknown/referrer/device categories are represented consistently.
- [x] Raw IP or other unnecessary sensitive event fields are not exposed.
- [x] OpenAPI documents query parameters and response schemas.
- [x] Backend build and representative manual queries succeed.
- [x] No automated test files are added.

## Verification

Query multiple ranges and links for two users, including an empty link and populated link, and verify ownership, range bounds, bucket ordering, and aggregate totals.

## Implementation and Verification Notes

- 2026-08-18: Added bearer-protected summary and time-series routes beneath
  `/api/v1/short-urls/{shortCode}/analytics`. Ownership is resolved before aggregate access, and an
  unknown, deleted, or differently owned short code returns the same `404 NOT_FOUND` response.
- Summary queries use version-1 daily aggregates for totals, daily unique estimates, bounded top
  referrers, and complete bounded device/browser/OS families. Time-series queries use hourly or
  daily overall aggregates and return ordered, zero-filled UTC buckets. SQL applies grouping,
  ordering, and the referrer limit server-side through the existing aggregate query index shape;
  raw access events are not queried or exposed.
- The contract uses exclusive `toUtc` ranges, explicit UTC alignment, a 366-day summary limit,
  744-hour and 731-day time-series limits, eventual-consistency timestamps, and open-bucket partial
  flags. Empty owned links return zero totals, empty category arrays, and zero-filled chart buckets.
  Full route, privacy, freshness, and unique-estimate semantics are in
  `docs/analytics-query-api.md`.
- A disposable LocalDB harness migrated a uniquely named database and verified two users, a
  populated link, an empty link, top-referrer truncation, aggregate totals (`10` clicks and `7`
  summed daily unique visitors), ordered hourly buckets (`0,3,0`), 30 default zero buckets, owner
  isolation, and oversized-range rejection. The database and harness were removed afterward; no
  automated test files were added.
- Live Swagger JSON exposed both routes with the documented parameters and response schemas, and
  an unauthenticated summary request returned the standard `401 AUTHENTICATION_REQUIRED` envelope.
  `dotnet build UrlShortener.sln --no-restore` succeeded with zero warnings and errors.
