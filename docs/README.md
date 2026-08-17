# Delivery Documentation

This directory is the execution plan for the product defined in `docs/PRD.md`.

## Active Phase

**Phase 08 — Asynchronous Click Analytics Pipeline**

Do not start a later phase until the active phase completion gate is satisfied, unless a documented dependency explicitly requires it.

## Phase Index

| Phase | Folder | Purpose |
|---|---|---|
| 00 | `phase-00-project-audit-foundation` | Establish a clean, measured, documented baseline. |
| 01 | `phase-01-architecture-modernization` | Restructure where justified and modernize the platform baseline. |
| 02 | `phase-02-identity-access-control` | Add secure identity, sessions/tokens, ownership, authorization. |
| 03 | `phase-03-owned-url-management` | Complete protected URL lifecycle and management APIs. |
| 04 | `phase-04-angular-foundation` | Create the Angular app foundation and reusable design system. |
| 05 | `phase-05-angular-core-product` | Deliver auth, dashboard, and URL-management UI workflows. |
| 06 | `phase-06-distributed-redirect-cache` | Make redirect caching multi-instance safe and measurable. |
| 07 | `phase-07-rate-limiting-resilience` | Add distributed policy-based limits and resilient API boundaries. |
| 08 | `phase-08-async-click-pipeline` | Remove analytics writes from redirect latency. |
| 09 | `phase-09-advanced-analytics` | Build richer analytics backend and Angular analytics UI. |
| 10 | `phase-10-developer-platform-api-keys` | Add scoped API keys and developer workflows. |
| 11 | `phase-11-custom-domains-qr` | Add domain verification/routing and QR capabilities. |
| 12 | `phase-12-security-privacy-abuse` | Harden security, privacy, and abuse controls. |
| 13 | `phase-13-background-jobs-data-lifecycle` | Add retention, cleanup, and scheduled maintenance. |
| 14 | `phase-14-observability-health` | Add telemetry, metrics, traces, and operational health. |
| 15 | `phase-15-containerization-deployment` | Produce reproducible local and deployment environments. |
| 16 | `phase-16-testing-performance` | Implement deferred automated tests and performance validation. |
| 17 | `phase-17-cicd-release-documentation` | Add quality gates, releases, ADR consolidation, and final docs. |

## Task Status Values

Use exactly one of:

- `Planned`
- `In Progress`
- `Blocked`
- `Completed`

## Reference Documents

- `authentication.md` describes account and session behavior.
- `authorization.md` defines protected management routes, owner-scoped access, and the 401/403/404 concealment policy.
- `management-api.md` is the finalized Angular-facing management resource, pagination/filter, error, UTC timestamp, and public URL contract.
- `idempotency-request-resilience.md` defines caller-scoped URL-create retries, retention,
  request/timeout bounds, safe dependency retry behavior, cancellation, and manual scenarios.
- `persistence.md` describes storage configuration and migration operations.
- `proxy-trust.md` defines direct and proxied client-IP derivation, trusted proxy/network
  configuration, IP normalization, and topology-change verification.
- `rate-limiting.md` defines distributed policies, identities, algorithms, configuration bounds,
  Redis state/expiry, `429` metadata, and outage behavior.
- `click-event-transport.md` defines RabbitMQ topology, configuration, delivery, retry/dead-letter,
  worker placement, and outage behavior.
- `redis.md` describes distributed-cache configuration, connection lifecycle, key namespaces, local setup, and outage behavior.
- `redirect-cache.md` defines redirect outcomes, payloads, TTL, invalidation, race safety,
  corruption handling, access recording, and Redis outage fallback.
- `resource-ownership.md` defines immutable ownership, legacy short-link treatment, and rules for future owned resources.

## Execution Rules

1. Read `docs/PRD.md` before working on any task.
2. Work on the lowest-numbered incomplete task in the active phase unless dependencies require another order.
3. Keep changes within task scope.
4. If a blocking design conflict is discovered, set the task to `Blocked`, document the conflict and evidence, and stop the conflicting implementation.
5. Automated test-file creation is deferred to Phase 16. Before then, use build checks and explicit manual/integration verification procedures without creating a parallel ad-hoc test suite.
6. Do not interpret the testing deferral as permission to ship untestable architecture.
7. A phase is complete only when every task in its folder is `Completed` and every phase completion gate is satisfied.
