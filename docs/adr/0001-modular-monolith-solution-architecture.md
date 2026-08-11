# ADR 0001: Modular Monolith Solution Architecture

- Status: Accepted
- Date: 2026-08-11
- Decision owners: URL Shortener maintainers
- Related task: TASK-005

## Context

The repository already separates the backend into Domain, Application, Infrastructure, and API projects. The product roadmap adds an Angular client and, later, independently hosted background processing, but does not require independently deployed business microservices.

The existing project references are acyclic:

```text
UrlShortener.Api ------------> UrlShortener.Infrastructure
       |                                  |
       v                                  v
UrlShortener.Application ----> UrlShortener.Domain
       |
       v
UrlShortener.Domain
```

Application use cases already consume repository, cache, clock, rate-limit, and code-generation interfaces rather than concrete implementations. Infrastructure owns EF Core, SQL Server, and in-memory adapters. API owns controllers, middleware, configuration, dependency injection, and process startup. No inspected dependency requires a project rename or source move.

## Decision

Keep the backend as a modular monolith with the existing four project names and responsibilities:

- `UrlShortener.Domain`: entities and domain rules that require no framework, persistence, HTTP, or UI implementation dependency.
- `UrlShortener.Application`: use cases, request/response models, validation, and ports required by those use cases. It may depend only on Domain.
- `UrlShortener.Infrastructure`: adapters for persistence, caching, time, identifiers, rate limiting, messaging, and other external systems. It may depend on Application and Domain.
- `UrlShortener.Api`: the HTTP host and composition root. It may depend on Application and Infrastructure; other projects must not depend on it.

Reserve these top-level locations for later roadmap phases:

- `web/`: the Angular workspace introduced in Phase 04.
- `workers/`: independently hosted background processes introduced when a roadmap task requires them. Shared business workflows remain in Application; worker projects act as composition/hosting boundaries.

New business capabilities should be organized by feature within these boundaries when their size justifies it. Interfaces are introduced at a replacement or test boundary, not for every class. A new deployable service or reversed project reference requires a separate ADR.

## Dependency Rules

1. Domain cannot reference Application, Infrastructure, API, `web`, or worker hosts.
2. Application cannot reference Infrastructure, API, `web`, or worker hosts.
3. Infrastructure cannot own controllers, HTTP response models, middleware, or UI behavior.
4. API and future worker hosts are composition roots; they wire concrete adapters to Application ports.
5. Angular consumes the documented HTTP contract and cannot become a source of backend business rules.
6. Cross-cutting infrastructure may be shared through narrowly scoped adapters, without exposing vendor-specific types through Application interfaces.
7. Project references must remain acyclic.

## Alternatives Considered

### Rename or reorganize all backend projects

Rejected. The current names accurately describe their roles, and the inspected references already follow the desired direction. A broad move would add review and migration cost without correcting a demonstrated boundary problem.

### Single-project vertical-slice API

Rejected for this roadmap. It would reduce project count but weaken the explicit replacement boundaries needed for SQL Server, Redis, queue-backed analytics, background workers, and later architecture tests.

### Microservices per capability

Rejected. The current product and team size do not justify distributed deployment and consistency costs. Independently hosted workers are allowed only where asynchronous processing requires a separate runtime process.

## Consequences

- Existing runtime behavior and public API contracts remain unchanged by this decision.
- No source files or project references need to move for TASK-005.
- Future persistence, cache, queue, and identity implementations plug into Application-owned boundaries.
- The API remains deployable as one process; future workers can scale separately without splitting core business ownership.
- Boundary enforcement is documented now and can be automated by architecture tests in Phase 16.

## Deferred Work

- The Angular project is created in Phase 04, not in this task.
- Worker projects and queue contracts are created in Phase 08 or another task that explicitly needs a worker.
- Automated architecture tests are deferred to Phase 16.
