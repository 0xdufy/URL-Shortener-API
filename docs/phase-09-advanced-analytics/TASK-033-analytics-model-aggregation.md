# TASK-033 — Analytics Data Model and Aggregation Strategy

**Status:** Planned  
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

- [ ] Supported dimensions and unsupported dimensions are explicitly listed.
- [ ] Analytics source-of-truth and aggregation flow are documented.
- [ ] Query strategy avoids scanning an unbounded raw access-log table for routine dashboard views.
- [ ] Aggregate uniqueness keys prevent duplicate aggregate rows for the same bucket/dimension.
- [ ] Late/retried events have documented aggregation behavior.
- [ ] Unknown device/browser/referrer values fall into stable categories rather than causing processing failure.
- [ ] Unique visitor design does not require storing raw IP indefinitely.
- [ ] Required schema/migrations/indexes are created and apply cleanly.
- [ ] Worker and backend builds succeed.
- [ ] No automated test files are added.

## Verification

Feed representative click events manually and inspect aggregate rows for multiple dates/referrers/device categories, including unknown metadata and a late event.