# TASK-023 — Redis Infrastructure and Distributed Cache Connection

**Status:** Completed
**Phase:** 06 — Distributed Redirect Cache

## Goal

Introduce Redis as a supported distributed dependency with explicit configuration and failure behavior, without yet mixing cache-policy refactors into connection setup.

## Dependencies

- Phase 05 completed.

## Scope

- Add the approved Redis client/provider and strongly typed configuration.
- Define connection lifecycle, timeouts, retry boundaries, key-prefix conventions, and environment configuration.
- Add local-development Redis instructions; Docker wiring may be provisional until Phase 15.
- Define application behavior when Redis is temporarily unavailable.
- Avoid placing domain/business logic in the Redis adapter.

## Acceptance Criteria

- [x] Redis connection settings are environment-configurable and validated.
- [x] No Redis credentials are committed to source control.
- [x] Redis client lifetime follows provider guidance and avoids per-request connection creation.
- [x] Timeouts/retries are bounded and documented.
- [x] Key namespace/prefix rules prevent ambiguous global keys.
- [x] Local development can start the backend with a documented Redis instance.
- [x] A temporary Redis outage has documented application behavior rather than indefinite request hangs.
- [x] Build and application startup succeed with Redis configured.
- [x] No automated test files are added.

## Verification

Start Redis, connect the application, verify a basic adapter operation through the intended cache abstraction, then observe/document configured failure behavior with Redis unavailable.

## Completion Notes

- Added the .NET 10 Redis distributed-cache provider and validated `RedisOptions` for endpoint,
  namespace, connect/operation timeouts, initial retries, and bounded reconnect delays.
- Registered the framework `IDistributedCache` provider as the application-lifetime Redis adapter.
  Its shared multiplexer is lazy, reconnects in the background, rejects command backlogging while
  disconnected, and is disposed with the host. `IShortUrlCache` remains unchanged for TASK-024.
- Enforced `application:environment:vN:` provider prefixes and documented feature-level key rules,
  secret injection, local Redis startup, lifecycle, retry, timeout, and outage behavior in
  `docs/redis.md`.
- `dotnet build UrlShortener.sln --artifacts-path .artifacts/task023` completed with zero warnings
  and zero errors on 2026-08-12.
- A temporary non-test smoke harness resolved the registered adapter twice as the same singleton,
  set/read `task-023-ok`, and confirmed a five-minute physical key under
  `url-shortener:development:v1:`. The key was removed after inspection.
- Against unused port 6399 with a 300 ms connect timeout, zero connect retries, a 200 ms operation
  timeout, and fail-fast backlog, the cache operation returned `RedisConnectionException` in 444
  ms. Host startup succeeded with both Redis available and unavailable, and an invalid prefix was
  rejected by startup options validation.
- No automated test files were added.
