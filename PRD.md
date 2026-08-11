# URL Shortener Platform — Product Requirements Document

**Status:** Approved planning baseline  
**Repository:** `0xdufy/URL-Shortener-API`  
**Primary implementation model:** phased delivery through `docs/phase-*` task files  
**Current phase:** Phase 00 — Project Audit & Engineering Foundation

## 1. Product Vision

Evolve the existing URL Shortener API into a production-oriented URL shortening and analytics platform with a robust ASP.NET Core backend, an Angular web application, authenticated user ownership, distributed caching, resilient redirect handling, asynchronous analytics processing, security controls, observability, containerized deployment, and a comprehensive automated test suite.

The end product should be strong enough to demonstrate real backend and full-stack engineering decisions rather than only CRUD functionality. The project should remain understandable and maintainable by one developer while still exposing realistic production concerns such as concurrency, distributed state, caching, background processing, authentication, authorization, telemetry, performance testing, and deployment.

## 2. Product Goals

1. Provide reliable creation, management, and redirection of short URLs.
2. Provide a complete Angular UI for authentication, link management, analytics, API keys, custom domains, and QR-code workflows.
3. Enforce ownership and authorization for all management operations.
4. Keep the redirect hot path fast and suitable for high read traffic.
5. Decouple analytics persistence from redirect latency.
6. Support multiple API instances by removing process-local assumptions where they affect correctness.
7. Provide useful analytics while minimizing unnecessary collection of personal data.
8. Expose a developer-friendly authenticated API with API-key support.
9. Make the system observable, deployable, and reproducible through Docker-based local infrastructure.
10. Finish with automated unit, integration, architecture, end-to-end, and load testing plus CI/CD quality gates.
11. Document important architectural decisions and measurable trade-offs.

## 3. Non-Goals

The following are explicitly out of scope unless a later ADR changes the decision:

- Splitting the system into many microservices solely for portfolio complexity.
- Building a billing/subscription system in the initial roadmap.
- Building a native mobile application.
- Building a full marketing CMS.
- Implementing social-network-style features.
- Adding features that do not improve the core URL-management, analytics, developer-platform, security, or operations story.

The preferred architecture is a modular monolith with clearly separated backend modules and, where justified, one or more worker processes for asynchronous workloads.

## 4. Existing Baseline

The repository already contains a .NET 8 ASP.NET Core Web API with separate Domain, Application, Infrastructure, and API projects. Existing functionality includes short URL creation, redirect resolution, custom aliases, expiry, active/inactive state, soft deletion, click tracking, basic daily statistics, SQL Server persistence, an in-memory repository mode, in-memory caching, an in-memory create-rate limiter, FluentValidation, AutoMapper, Swagger, Serilog, and a consistent error envelope.

This baseline is to be preserved where it is correct, but the implementation may be restructured when the architecture audit identifies a concrete maintainability, correctness, performance, or deployment reason.

## 5. Target Technical Direction

### 5.1 Backend

- ASP.NET Core on the target .NET version selected and documented during Phase 01.
- EF Core with SQL Server as the primary relational persistence engine.
- Clear separation among Domain, Application, Infrastructure, API, and worker responsibilities.
- Authentication and authorization with user ownership of managed resources.
- Redis-backed distributed capabilities where process-local state would fail in multi-instance deployment.
- Asynchronous click/analytics processing through an explicit queue abstraction and worker.
- Background jobs for retention, cleanup, and scheduled operational work.
- OpenTelemetry-compatible logs, metrics, and traces.
- Health/readiness endpoints for operational dependencies.

### 5.2 Frontend

- Angular SPA as a first-class product surface.
- Typed API client layer and centralized HTTP/error handling.
- Route guards and authentication state management.
- Responsive application shell and reusable design system.
- Feature areas for Dashboard, Links, Link Details, Analytics, API Keys, Custom Domains, QR Codes, Account, and operational error states.
- Accessibility and keyboard navigation considered in acceptance criteria.
- Loading, empty, error, success, unauthorized, forbidden, and rate-limited states must be intentionally designed.

### 5.3 Local and Production-like Infrastructure

- Docker-based local stack.
- SQL Server.
- Redis.
- Queue technology selected by ADR when asynchronous analytics is implemented.
- Metrics/visualization dependencies introduced only when their phase requires them.
- Configuration supplied through environment-aware configuration and secrets, not committed credentials.

## 6. Target High-Level Architecture

```text
Browser / API Client
        |
        +-----------------------+
        |                       |
        v                       v
   Angular Web              Public API
        |                       |
        +-----------+-----------+
                    |
                    v
             ASP.NET Core API
                    |
       +------------+-------------+
       |            |             |
       v            v             v
   SQL Server     Redis      Event/Queue Layer
                                  |
                                  v
                           Analytics Worker
                                  |
                                  v
                             SQL Server

Cross-cutting: Authentication, Authorization, Rate Limiting,
OpenTelemetry, Health Checks, Security, Configuration.
```

The exact project boundaries may change during Phase 01. Any structural change must be justified and recorded rather than performed as an aesthetic refactor.

## 7. Primary Personas

### 7.1 Anonymous Visitor

May create a limited short URL only if anonymous shortening remains enabled. Receives stricter limits and no private management capabilities unless a safe ownership mechanism is explicitly designed.

### 7.2 Registered User

Can create and manage owned links, inspect analytics, manage API keys, configure supported custom domains, generate QR codes, and manage account/session state.

### 7.3 API Consumer

Uses scoped API keys to create/read/manage allowed resources programmatically under explicit rate limits.

### 7.4 Operator / Maintainer

Needs health signals, logs, metrics, traces, predictable configuration, migration procedures, deployment documentation, and failure diagnostics.

## 8. Functional Requirements

### FR-01 — Identity and Session Management

The system shall provide secure registration/login/session/logout flows or an ADR-approved equivalent. Authentication errors must not expose secrets. Protected routes and API endpoints must require a valid identity.

### FR-02 — Ownership and Authorization

Every managed URL shall have an owner or an explicitly documented anonymous-ownership model. A user must never be able to inspect analytics, mutate, restore, or delete another user's protected resource.

### FR-03 — URL Creation

Authenticated users shall create short links from valid absolute HTTP/HTTPS URLs, optionally using a permitted custom alias and expiry. Alias uniqueness must be concurrency-safe at the database boundary.

### FR-04 — URL Management

Users shall list, search, filter, sort, inspect, activate/deactivate, edit supported properties, soft delete, and restore owned links. Pagination must be bounded and deterministic.

### FR-05 — Redirect Resolution

`GET /r/{shortCode}` shall resolve active, non-deleted, non-expired links and return an HTTP redirect. Expired, deleted, inactive, and unknown states must have documented behavior. Redirect latency must not synchronously depend on analytics writes after the asynchronous pipeline is introduced.

### FR-06 — Distributed Caching

Redirect lookup caching must remain correct when multiple API instances are running. Cache invalidation must cover mutations affecting redirect behavior.

### FR-07 — Rate Limiting

The service shall support policy-based rate limits for anonymous callers, authenticated users, API keys, and sensitive endpoints. Limits that affect multi-instance correctness must use distributed state or another documented strategy.

### FR-08 — Click Event Processing

Successful redirects shall emit click events through a queue abstraction. Persistence of click analytics shall be handled asynchronously with retry/idempotency requirements documented.

### FR-09 — Analytics

Users shall be able to inspect useful aggregate analytics such as total clicks, trends, referrers, device/browser/OS categories, and privacy-appropriate unique visitor estimates where implemented. Analytics must document freshness/consistency expectations.

### FR-10 — API Keys

Users shall create, name, scope, inspect metadata for, rotate/revoke, and securely use API keys. Plaintext secrets shall only be shown at creation/rotation time and must not be stored in recoverable plaintext.

### FR-11 — Custom Domains

Users shall be able to register supported custom domains, receive ownership-verification instructions, view verification state, and use verified domains for link generation where routing/deployment permits it.

### FR-12 — QR Codes

Users shall generate QR representations for owned short links in supported formats without duplicating link ownership logic.

### FR-13 — Abuse Controls

The system shall support reserved aliases, blocked/unsafe destination policies, reporting/moderation hooks where appropriate, and limits that reduce obvious abuse without pretending to provide perfect phishing detection.

### FR-14 — Data Lifecycle

Expired/deleted records and analytics data shall have explicit retention rules. Cleanup jobs must be safe to rerun and observable.

### FR-15 — Angular Application

The Angular application shall provide complete user workflows for the supported product functions and shall not depend on Swagger as the primary user experience.

## 9. Angular Information Architecture

Target routes may evolve, but the product should converge on a structure similar to:

```text
/
/login
/register
/app
/app/dashboard
/app/links
/app/links/new
/app/links/:shortCode
/app/links/:shortCode/analytics
/app/api-keys
/app/domains
/app/account
```

Key UI requirements:

- Responsive application shell.
- Consistent navigation and page titles.
- Reusable tables, filters, pagination, confirmation dialogs, forms, status badges, toasts/notifications, skeleton/loading states, and empty states.
- Clear distinction between destination URL and short URL.
- Copy-to-clipboard actions with feedback.
- QR-code presentation/download workflow where supported.
- Analytics visualizations that remain understandable without color alone.
- Friendly handling of 400/401/403/404/409/410/429/5xx responses.
- No security-sensitive data stored in inappropriate browser storage solely for convenience.

## 10. Data Model Direction

The final schema is determined through migrations and ADRs, but expected concepts include:

- `User`
- `ShortUrl`
- `ShortUrlAccessLog` and/or normalized/aggregated analytics records
- `ApiKey`
- `CustomDomain`
- optional `DomainVerificationAttempt`
- optional `AbuseReport`/moderation metadata
- background-processing metadata only when the selected infrastructure requires it

`ShortUrl` should contain a stable owner relationship for authenticated resources. Database uniqueness constraints remain authoritative for short codes and other unique identifiers.

## 11. API Design Requirements

- Versioned API surface.
- Consistent structured error contract.
- CancellationToken propagation for I/O-bound operations.
- Bounded pagination.
- Consistent UTC handling.
- Explicit 401 vs 403 semantics.
- Concurrency-safe unique resource creation.
- No business logic duplicated between controllers and Angular.
- OpenAPI documentation kept accurate as the contract evolves.
- Breaking API changes require an explicit task/ADR.

## 12. Security Requirements

- Authentication/session/token strategy documented before implementation.
- Passwords, tokens, and API-key secrets stored only using appropriate one-way hashing or provider-approved secure storage.
- Secrets excluded from source control and logs.
- Authorization checked server-side for every protected resource.
- Rate limits for authentication and creation endpoints.
- Input validation for destination URLs and aliases.
- Reserved routes/aliases cannot be shadowed by user aliases.
- CORS/trusted-origin configuration is explicit.
- Proxy/client-IP trust is explicit in deployment configuration.
- Security headers configured where applicable.
- Raw client IP retention minimized; privacy-preserving identifiers preferred where analytics requirements permit.
- Logs must avoid credential, token, API-key, and sensitive header leakage.

## 13. Performance and Reliability Requirements

Performance numbers shall be measured rather than invented. The project must eventually include reproducible load scenarios and recorded p50/p95/p99 latency and throughput.

Design objectives:

- Redirect path optimized for reads.
- Analytics writes removed from the synchronous redirect critical path.
- Cache strategy documented and measured.
- Database indexes reviewed against real queries.
- Queue processing supports retry/backpressure/failure visibility.
- Graceful cancellation/shutdown for API and workers.
- Dependency failures return controlled behavior rather than corrupting state.

## 14. Observability Requirements

The deployed system should expose:

- Structured application logs.
- Request correlation/trace identifiers.
- Distributed tracing where meaningful.
- Metrics for request count, latency, errors, redirects, cache hits/misses, queue depth/processing, and dependency health where supported.
- Liveness and readiness checks.
- Dashboards/documentation sufficient to diagnose common failure modes.

## 15. Testing Strategy

Automated test-file implementation is intentionally deferred until the dedicated testing phase, per project planning decision. This does **not** remove quality requirements from earlier phases.

Before the testing phase:

- Every task must define testable acceptance criteria.
- Components must be designed with replaceable external dependencies and deterministic boundaries where reasonable.
- Manual verification commands/scenarios may be documented.
- New code must not deliberately introduce structures that make later integration testing impractical.

The dedicated testing phase will add:

- Unit tests.
- Integration/API tests.
- Architecture tests.
- Angular component/service tests where they add value.
- Angular end-to-end tests for critical workflows.
- Concurrency tests.
- Container-backed dependency tests.
- Load/performance tests.

## 16. Delivery Phases

| Phase | Name | Outcome |
|---|---|---|
| 00 | Project Audit & Engineering Foundation | Clean, documented baseline and agreed technical constraints. |
| 01 | Solution Architecture & Platform Modernization | Target project structure and runtime/platform baseline. |
| 02 | Identity & Access Control | Secure user identity, sessions/tokens, ownership primitives. |
| 03 | Owned URL Lifecycle & Management API | Complete protected link-management capabilities. |
| 04 | Angular Foundation & Design System | Maintainable Angular application shell and API integration foundation. |
| 05 | Angular Auth, Dashboard & Link Management | End-user authentication and core URL-management workflows. |
| 06 | Distributed Redirect Cache | Multi-instance-safe redirect caching and invalidation. |
| 07 | Distributed Rate Limiting & API Resilience | Policy-based limits and resilient API boundaries. |
| 08 | Asynchronous Click Pipeline | Redirect hot path decoupled from analytics persistence. |
| 09 | Advanced Analytics & Analytics UI | Rich aggregates and usable Angular analytics views. |
| 10 | Developer Platform & API Keys | Scoped programmatic access and API-key management UI. |
| 11 | Custom Domains & QR Codes | Branded link domains and QR workflows. |
| 12 | Security, Privacy & Abuse Hardening | Explicit production-oriented security/privacy controls. |
| 13 | Background Jobs & Data Lifecycle | Retention, cleanup, expiry, and scheduled maintenance. |
| 14 | Observability & Operational Health | Logs, metrics, traces, health checks, dashboards. |
| 15 | Containerization & Deployment Baseline | Reproducible local/prod-like stack and deployment configuration. |
| 16 | Automated Testing & Performance Validation | Full test pyramid, E2E, concurrency, and load evidence. |
| 17 | CI/CD, Release Engineering & Final Documentation | Automated gates, release workflow, ADRs, portfolio-quality docs. |

Only one phase should be considered the active implementation phase at a time unless a dependency explicitly requires parallel work.

## 17. Task Execution Rules

1. Read this PRD, the active phase task files, and relevant existing code before making changes.
2. Do not implement tasks from later phases opportunistically unless required to avoid a correctness regression.
3. If the current architecture conflicts with a task, document the conflict and choose the smallest justified structural change.
4. Do not silently change public API contracts, persistence semantics, authentication strategy, or deployment assumptions.
5. Record material architectural decisions as ADRs when the relevant phase calls for them.
6. Preserve backward compatibility when practical; otherwise document migration/breaking behavior.
7. Keep each task reviewable and scoped.
8. Satisfy every acceptance criterion before marking a task complete.
9. Automated test files are deferred to Phase 16, but testability and manual verification remain mandatory earlier.
10. After completing a task, update its status and any phase-level tracking file created by the task plan.

## 18. Definition of Done for a Task

A task is complete only when:

- All in-scope implementation is present.
- All stated acceptance criteria are satisfied.
- Build succeeds for affected projects.
- No new compiler errors or intentionally ignored critical warnings are introduced.
- Configuration and migrations required by the task are included.
- API/OpenAPI or UI behavior is updated where the task changes a contract.
- Relevant documentation is updated.
- Manual verification steps have been performed when automated tests are not yet available.
- No secrets, build artifacts, or machine-local files are committed.
- The task file status is updated with a concise implementation/verification record.

## 19. Definition of Done for the Product Roadmap

The roadmap is complete when a fresh environment can build and run the documented stack, a user can authenticate and manage short links through Angular, redirect traffic uses the documented distributed/async architecture, analytics are visible, protected APIs enforce ownership, operational telemetry and health checks are available, automated tests and performance scenarios pass through CI, and the repository contains sufficient architecture/operations documentation for another engineer to understand and run the system.
