# TASK-048 — Background Job Scheduling Foundation

**Status:** Planned  
**Phase:** 13 — Background Jobs & Data Lifecycle

## Goal

Introduce a deliberate scheduling mechanism for maintenance work that is separate from the click-event consumer and safe under multiple application instances.

## Dependencies

- Phase 12 completed.

## Scope

- Decide whether maintenance jobs run in the existing worker host or through an approved scheduler library/service.
- Document distributed locking/single-execution requirements for jobs that must not run concurrently.
- Define job cancellation, retries, timeout, failure recording, and operator visibility.
- Keep job registration/configuration centralized.
- Do not run long maintenance loops synchronously during API startup.

## Acceptance Criteria

- [ ] Scheduler/worker placement is documented and consistent with the architecture ADRs.
- [ ] Multi-instance deployment cannot unintentionally execute single-owner maintenance work concurrently without a documented safe strategy.
- [ ] Jobs have bounded retries/timeouts and cancellation handling.
- [ ] A failed job is visible through logs/status suitable for Phase 14 telemetry.
- [ ] API startup does not block on long cleanup operations.
- [ ] Job schedules are environment-configurable where appropriate.
- [ ] Scheduler credentials/state are not hardcoded.
- [ ] Worker/backend builds succeed and one harmless scheduled job can be manually observed.
- [ ] No automated test files are added.

## Verification

Run multiple worker/API instances as applicable and demonstrate the selected distributed-execution behavior for one sample maintenance operation.