# TASK-052 — OpenTelemetry Tracing

**Status:** Planned  
**Phase:** 14 — Observability & Operational Health

## Goal

Instrument API, persistence/cache calls, event publication/consumption, and worker processing with OpenTelemetry-compatible traces that explain latency and failure boundaries.

## Dependencies

- TASK-051 completed.

## Scope

- Add OpenTelemetry tracing setup to API and worker hosts.
- Instrument ASP.NET Core requests and approved SQL/Redis/client dependencies.
- Propagate trace context through the selected queue transport when technically appropriate.
- Add custom spans around redirect resolution, event publication, analytics processing, and maintenance jobs only where they provide diagnostic value.
- Configure exporter through environment settings; local console/OTLP options may differ by environment.
- Avoid recording full URLs, secrets, raw auth headers, or unbounded high-cardinality attributes.

## Acceptance Criteria

- [ ] A management API request produces a trace with meaningful server/dependency spans.
- [ ] A redirect trace distinguishes cache hit/miss/fallback and event-publication boundaries without exposing sensitive payload data.
- [ ] Click-event trace context can be related to worker processing where the transport supports propagation.
- [ ] SQL/Redis span collection is configured with privacy/cardinality safeguards.
- [ ] Exporter endpoint/credentials are configuration-driven.
- [ ] Telemetry failure does not break core product requests.
- [ ] Sampling strategy is documented for high-volume redirect traffic.
- [ ] API/worker builds and local trace export succeed.
- [ ] No automated test files are added.

## Verification

Capture representative traces for a cache hit, cache miss with SQL fallback, URL creation, and worker event consumption, and confirm the critical latency boundaries are visible.