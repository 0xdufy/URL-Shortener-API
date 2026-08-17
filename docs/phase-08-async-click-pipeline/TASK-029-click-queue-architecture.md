# TASK-029 — Click Event Queue Architecture

**Status:** In Progress
**Phase:** 08 — Asynchronous Click Pipeline

## Goal

Choose and introduce the asynchronous transport used to remove analytics persistence from the redirect critical path.

## Dependencies

- Phase 07 completed.

## Scope

- Compare a process-local `Channel<T>` transitional design with a durable distributed broker/stream appropriate for the target deployment.
- Select the production target and document it in an ADR, including delivery semantics, durability, ordering needs, retry model, dead-letter/failure handling, and operational cost.
- Introduce transport configuration and connection lifecycle.
- Define an application-facing event publisher abstraction that does not leak broker-specific APIs into redirect/domain logic.
- Reserve a worker host/project if the target architecture requires a separate process.

## Acceptance Criteria

- [x] ADR explains why the selected transport fits this project's reliability and portfolio goals.
- [x] Delivery semantics are stated explicitly; wording such as "exactly once" is not used unless actually guaranteed end-to-end.
- [x] Publisher/consumer boundaries are broker-agnostic at application level where practical.
- [x] Connection, timeout, retry, and credential configuration are explicit and environment-driven.
- [x] A worker process/location exists or is clearly prepared for TASK-031.
- [x] Broker outage behavior is documented before redirect integration occurs.
- [x] No analytics business logic is implemented inside transport plumbing.
- [x] Backend/worker builds succeed.
- [x] No automated test files are added.

## Verification

Start the selected transport locally, establish publisher and consumer connectivity through the new abstraction, and document observed behavior when the transport is unavailable.

## Implementation and Verification Notes

- 2026-08-17: Accepted ADR 0004 and selected RabbitMQ durable quorum queues with publisher
  confirms, manual consumer acknowledgements, bounded redelivery, and a durable dead-letter quorum
  queue. Application owns provider-neutral publication/consumption contracts; RabbitMQ client types
  remain in Infrastructure.
- Added validated, environment-driven endpoint, credential, TLS, timeout, heartbeat, recovery,
  prefetch, retry, delivery-limit, and topology configuration. Base production credentials are empty
  and fail startup validation; Development uses the loopback-only `guest` account.
- Added the independently hosted `UrlShortener.Analytics.Worker` composition root reserved for
  TASK-031. Startup establishes broker connectivity and declares compatible topology without adding
  analytics business logic.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. No automated
  test files were added.
- With no broker listening on `127.0.0.1:5672`, the Development worker failed startup without an
  in-memory fallback. Production startup with empty credentials failed options validation before a
  connection attempt, as intended.
- Remaining verification: start RabbitMQ locally and demonstrate publisher-confirmed handoff plus
  consumer connectivity/acknowledgement through the new abstractions. Docker Desktop could not start
  because of a pre-existing stale runtime socket. The socket was recoverably renamed to
  `dockerInference.task029-stale-20260817`, but Docker restart verification was interrupted before a
  RabbitMQ container could be created. Keep this task `In Progress` until the live broker path is
  observed.
