# TASK-034 — Owner-Scoped Analytics Query API

**Status:** Planned  
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

- [ ] Only the link owner can access protected analytics.
- [ ] Date ranges are bounded and validated.
- [ ] Time buckets use documented UTC boundaries.
- [ ] API exposes analytics freshness/eventual-consistency metadata when useful.
- [ ] Routine queries use aggregate/indexed data rather than unbounded raw scans.
- [ ] Empty analytics returns a valid empty model rather than an avoidable server error.
- [ ] Unknown/referrer/device categories are represented consistently.
- [ ] Raw IP or other unnecessary sensitive event fields are not exposed.
- [ ] OpenAPI documents query parameters and response schemas.
- [ ] Backend build and representative manual queries succeed.
- [ ] No automated test files are added.

## Verification

Query multiple ranges and links for two users, including an empty link and populated link, and verify ownership, range bounds, bucket ordering, and aggregate totals.