# TASK-033 — Analytics Data Model and Aggregation Strategy

**Status:** Completed
**Phase:** 09 — Advanced Analytics & Analytics UI

## Goal

Define a query-efficient analytics model that supports useful product insights without turning raw click storage into an unbounded or privacy-insensitive log.

## Dependencies

- Phase 08 completed.

## Scope

- Define supported analytics dimensions: total/trend by time, referrer/source, device class, browser family, OS family, and privacy-approved unique-visitor estimate.
- Decide which data is stored raw, normalized, pre-aggregated, or computed on query.
- Define hourly/daily aggregation granularity and late-event handling.
- Define indexes and retention implications.
- Define how unknown/unparseable user-agent/referrer values are categorized.
- Geographic enrichment is optional and requires an explicit data source/privacy decision; do not invent location from unreliable data.

## Acceptance Criteria

- [x] Supported dimensions and unsupported dimensions are explicitly listed.
- [x] Analytics source-of-truth and aggregation flow are documented.
- [x] Query strategy avoids scanning an unbounded raw access-log table for routine dashboard views.
- [x] Aggregate uniqueness keys prevent duplicate aggregate rows for the same bucket/dimension.
- [x] Late/retried events have documented aggregation behavior.
- [x] Unknown device/browser/referrer values fall into stable categories rather than causing processing failure.
- [x] Unique visitor design does not require storing raw IP indefinitely.
- [x] Required schema/migrations/indexes are created and apply cleanly.
- [x] Worker and backend builds succeed.
- [x] No automated test files are added.

## Verification

Feed representative click events manually and inspect aggregate rows for multiple dates/referrers/device categories, including unknown metadata and a late event.

## Implementation and Verification Notes

- 2026-08-18: Added versioned hourly/daily aggregate rows and daily pseudonymous visitor keys.
  Composite primary keys are the duplicate-row boundary, and serializable range-protected updates
  keep concurrent first increments safe. Worker rollups participate in the existing access-log and
  counter transaction, so an event-ID conflict rolls every projection back.
- Dimension schema version 1 provides bounded referrer, device, browser, and OS values. Missing or
  malformed metadata maps to stable `Direct`, `Unknown`, or `Other` buckets without failing event
  handling. The full supported/unsupported contract, query path, late-event semantics, and
  retention targets are in `docs/analytics-data-model.md`.
- Migration `AddAnalyticsAggregationModel` applied through the complete migration chain to a clean,
  disposable LocalDB database. A second database was stopped at the preceding migration, seeded
  with three historical access rows, and upgraded successfully; backfilled hourly/daily,
  dimension, and visitor rows matched the source events.
- A disposable manual harness processed four unique events across two dates, including a late event,
  two referrers, mobile/desktop/unknown metadata, and two clicks from one same-day visitor. It
  produced `logs=4`, `ClickCount=4`, three daily visitor keys, correct hourly/daily rows, daily
  unique counts of `2` and `1`, and stable `Direct`/`Unknown` buckets. Replaying one event ID left
  every count unchanged. The harness and database were removed; no automated test files were added.
- `dotnet build UrlShortener.sln --no-restore` succeeded with zero warnings and errors.
