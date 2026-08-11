# TASK-056 — Reproducible Docker Compose Development Stack

**Status:** Planned  
**Phase:** 15 — Containerization & Deployment Baseline

## Goal

Make the complete production-like local environment start through one documented Compose workflow with persistent dependencies and clear health ordering.

## Dependencies

- TASK-055 completed.

## Scope

Create Compose configuration for the components actually selected by previous phases, including:

- API.
- Angular web host.
- Worker.
- SQL Server.
- Redis.
- Selected event queue/broker.
- Observability services such as Prometheus/Grafana/OTLP collector where Phase 14 chose them.

Use named volumes where persistence is expected and health-based dependency behavior rather than arbitrary sleeps where possible.

## Acceptance Criteria

- [ ] `docker compose up` or the documented equivalent starts the required local stack from a clean environment.
- [ ] Service names/ports/networks are documented and avoid unnecessary host exposure.
- [ ] SQL/Redis/broker data uses intentional persistence volumes for normal local development.
- [ ] Health checks reflect Phase 14 semantics.
- [ ] API and worker do not start successfully with silently missing mandatory configuration.
- [ ] Development secrets/default credentials are clearly local-only and not reused as production recommendations.
- [ ] Angular reaches the API through the documented browser-accessible routing/base URL.
- [ ] Restarting one API instance does not destroy shared SQL/Redis/queue state.
- [ ] No automated test files are added.

## Verification

Start the full stack, create/sign in/manage a link, perform redirects, observe analytics worker processing and telemetry, restart selected stateless services, and verify state remains consistent.