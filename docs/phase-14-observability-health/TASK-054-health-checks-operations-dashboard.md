# TASK-054 — Health Checks and Operations Dashboard

**Status:** Planned  
**Phase:** 14 — Observability & Operational Health

## Goal

Expose correct liveness/readiness signals and provide an operational dashboard/runbook that makes dependency and throughput problems diagnosable.

## Dependencies

- TASK-053 completed.

## Scope

- Add separate liveness and readiness endpoints.
- Readiness should reflect dependencies required for the host to serve its intended role: SQL Server, Redis, queue, and worker-specific dependencies as appropriate.
- Liveness must not fail merely because a downstream dependency has a temporary outage if the process itself is healthy.
- Define health endpoint exposure/security and response detail by environment.
- Add a Prometheus/Grafana-compatible dashboard or approved equivalent showing the metrics from TASK-053.
- Create an operations runbook for common failure cases.

## Acceptance Criteria

- [ ] `/health/live` or approved equivalent reports process liveness without expensive dependency checks.
- [ ] `/health/ready` reports unavailable when a dependency essential to the host's role is unavailable.
- [ ] Health checks use bounded timeouts and do not create dependency load spikes.
- [ ] Public health response does not expose secrets/connection strings/internal stack traces.
- [ ] Dashboard includes request/redirect latency and errors, cache effectiveness, queue/worker health, and maintenance outcomes where metrics exist.
- [ ] Runbook explains at least SQL outage, Redis outage, queue outage/backlog, worker failure, and migration/configuration failure symptoms and first diagnostic steps.
- [ ] Health/readiness semantics are documented for container orchestration/reverse proxy use.
- [ ] API/worker builds and dependency-outage manual checks succeed.
- [ ] No automated test files are added.

## Phase 14 Completion Gate

Phase 14 is complete when TASK-051 through TASK-054 are completed and API/worker behavior can be correlated, traced, measured, health-checked, and diagnosed through documented operational tooling.