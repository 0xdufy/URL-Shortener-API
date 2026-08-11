# TASK-053 — Metrics and Service Indicators

**Status:** Planned  
**Phase:** 14 — Observability & Operational Health

## Goal

Expose bounded, actionable metrics that measure redirect performance, application reliability, cache effectiveness, queue health, and maintenance outcomes.

## Dependencies

- TASK-052 completed.

## Scope

- Define metrics for request rate/latency/errors, redirect outcomes, cache hit/miss/error, event publish/consume/failure, queue lag/depth where available, analytics processing latency, and maintenance job results.
- Define histograms/buckets appropriate for redirect/API latency.
- Avoid labels with short code, URL, user ID, raw host, IP, or other unbounded cardinality unless strongly justified.
- Expose metrics through an approved OpenTelemetry/Prometheus-compatible path.
- Document target service indicators; numeric SLO targets may be provisional until Phase 16 load results exist.

## Acceptance Criteria

- [ ] Redirect request count and latency are measurable.
- [ ] Cache hit/miss/error ratio is measurable without high-cardinality labels.
- [ ] Event publication, consumption, retry/failure, and processing latency are measurable.
- [ ] Maintenance job duration/result counts are measurable.
- [ ] Metric labels are reviewed for bounded cardinality.
- [ ] Metrics endpoint/export is protected according to deployment topology and is not treated as a public user endpoint by default.
- [ ] Telemetry collection failure does not fail core product requests.
- [ ] Proposed service indicators are documented for later performance baselining.
- [ ] API/worker builds and local metric scraping/export succeed.
- [ ] No automated test files are added.

## Verification

Generate representative API, redirect, cache-miss, worker, and maintenance activity; inspect exported metrics and verify label cardinality stays bounded.