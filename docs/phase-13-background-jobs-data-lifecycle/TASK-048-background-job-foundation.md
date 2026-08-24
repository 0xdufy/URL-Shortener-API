# TASK-048 — Background Job Scheduling Foundation

**Status:** Completed
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

- [x] Scheduler/worker placement is documented and consistent with the architecture ADRs.
- [x] Multi-instance deployment cannot unintentionally execute single-owner maintenance work concurrently without a documented safe strategy.
- [x] Jobs have bounded retries/timeouts and cancellation handling.
- [x] A failed job is visible through logs/status suitable for Phase 14 telemetry.
- [x] API startup does not block on long cleanup operations.
- [x] Job schedules are environment-configurable where appropriate.
- [x] Scheduler credentials/state are not hardcoded.
- [x] Worker/backend builds succeed and one harmless scheduled job can be manually observed.
- [x] No automated test files are added.

## Verification

Run multiple worker/API instances as applicable and demonstrate the selected distributed-execution behavior for one sample maintenance operation.

## Completion Notes

- 2026-08-24: Accepted ADR 0005. Maintenance scheduling is a separate hosted service in the existing
  analytics worker; the API composition root remains unchanged.
- Added centralized, validated job configuration, cooperative per-attempt timeouts, bounded retry
  delays/counts, shutdown cancellation, and structured run/attempt/outcome/duration logs.
- Added a SQL Server `sp_getapplock` adapter whose session-scoped exclusive ownership spans all retry
  attempts and is automatically released on connection/process loss. Lock credentials come from the
  worker's existing environment-supplied SQL connection string.
- Added the disabled-by-default `foundation-heartbeat` job and enabled it only in Development for
  harmless manual observation. No automated test files were added.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors. A
  disposable manual LocalDB harness opened independent SQL sessions and confirmed that a concurrent
  owner was excluded, session disposal allowed reacquisition, and the harmless heartbeat executed
  visibly. The harness was removed afterward; no automated test files remain.
