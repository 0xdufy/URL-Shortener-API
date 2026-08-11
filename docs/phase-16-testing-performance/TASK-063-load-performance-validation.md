# TASK-063 — Load, Concurrency, and Performance Validation

**Status:** Planned  
**Phase:** 16 — Automated Testing & Performance Validation

## Goal

Measure the architecture under reproducible traffic and document real throughput/latency evidence rather than claiming performance from design alone.

## Dependencies

- TASK-062 completed.

## Scope

- Add k6 or an approved load-testing tool and versioned scenarios.
- Create separate scenarios for redirect-heavy traffic, authenticated URL creation/management, and analytics reads.
- Include warm-cache and cold-cache redirect scenarios.
- Include stepped traffic levels and a safe stress/failure threshold for the local/test environment.
- Capture p50/p95/p99 latency, throughput, error rate, cache hit ratio, database/Redis/broker behavior, and queue lag/worker throughput where telemetry supports it.
- Compare asynchronous redirect architecture against documented Phase 00/available baseline evidence when a fair comparison is possible.
- Add concurrency-focused creation tests for uniqueness and rate limiting.

## Acceptance Criteria

- [ ] Load scripts are committed, configurable, and runnable without editing source for environment URLs/credentials.
- [ ] Redirect scenario separates valid redirects from expected 404/410 outcomes in metrics.
- [ ] Results include p50/p95/p99, throughput, and error rate rather than averages alone.
- [ ] Cache hit/miss and queue lag are observed alongside request latency when diagnosing bottlenecks.
- [ ] Test data generation avoids one hot short code being mistaken for realistic aggregate system performance unless that scenario is intentionally labeled.
- [ ] Performance claims in README use measured results from a documented environment and date.
- [ ] No fabricated target or result is recorded as achieved.
- [ ] Resource saturation/bottleneck observations are documented with evidence.
- [ ] A repeatable baseline report is added to `docs/performance.md`.

## Phase 16 Completion Gate

Phase 16 is complete when TASK-059 through TASK-063 are completed, automated backend/frontend/E2E suites are repeatable, concurrency invariants are covered, and performance evidence is documented from reproducible scenarios.