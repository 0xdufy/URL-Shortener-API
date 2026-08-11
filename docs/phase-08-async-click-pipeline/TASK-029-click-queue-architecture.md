# TASK-029 — Click Event Queue Architecture

**Status:** Planned  
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

- [ ] ADR explains why the selected transport fits this project's reliability and portfolio goals.
- [ ] Delivery semantics are stated explicitly; wording such as "exactly once" is not used unless actually guaranteed end-to-end.
- [ ] Publisher/consumer boundaries are broker-agnostic at application level where practical.
- [ ] Connection, timeout, retry, and credential configuration are explicit and environment-driven.
- [ ] A worker process/location exists or is clearly prepared for TASK-031.
- [ ] Broker outage behavior is documented before redirect integration occurs.
- [ ] No analytics business logic is implemented inside transport plumbing.
- [ ] Backend/worker builds succeed.
- [ ] No automated test files are added.

## Verification

Start the selected transport locally, establish publisher and consumer connectivity through the new abstraction, and document observed behavior when the transport is unavailable.