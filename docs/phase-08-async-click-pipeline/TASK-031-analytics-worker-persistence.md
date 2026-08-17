# TASK-031 — Analytics Worker, Idempotency, and Persistence

**Status:** In Progress
**Phase:** 08 — Asynchronous Click Pipeline

## Goal

Consume click events reliably and update analytics persistence without double-counting retried deliveries.

## Dependencies

- TASK-030 completed.

## Scope

- Implement worker consumption with graceful startup/shutdown and cancellation.
- Persist click events and/or update aggregates according to the analytics data model.
- Implement idempotent processing using event ID or an equivalent database-enforced strategy.
- Define transactional boundary for event record, counters, and aggregate updates.
- Define retry and poison/dead-letter behavior according to TASK-029 ADR.
- Avoid one database transaction per event when safe batching materially improves throughput; batching is optional and must preserve correctness.

## Acceptance Criteria

- [x] Re-delivery of the same event does not increment analytics more than once.
- [x] Database uniqueness/transaction rules support the idempotency guarantee.
- [x] Worker failures do not acknowledge/drop an event contrary to the selected delivery semantics.
- [x] Poison/failing messages have a bounded retry path and visible disposition.
- [x] Worker handles graceful shutdown without intentionally abandoning acknowledged-but-unpersisted work.
- [x] Click count/last-access metadata has one documented source of truth.
- [x] Worker does not require HTTP controller context.
- [x] Database indexes support expected event/aggregate queries.
- [x] Worker build and manual duplicate-event verification succeed.
- [x] No automated test files are added.

## Verification

Publish the same event ID more than once, verify only one logical click is persisted/counted, and manually exercise transient consumer failure/retry behavior.

## Implementation and Verification Notes

- 2026-08-17: Added the independently hosted click consumer. Each delivery gets a fresh dependency
  injection scope and invokes an Application handler; neither the workflow nor persistence adapter
  requires HTTP request/controller state. Contract-invalid and missing-link events are permanent
  failures, while unexpected exceptions retain the ADR's bounded retry/dead-letter behavior.
- The stable event ID is the `ShortUrlAccessLogs` primary key. One SQL transaction performs the
  atomic counter increment, monotonic last-access update, and privacy-approved access-log insert.
  A concurrent primary-key conflict rolls the transaction back and is acknowledged only after the
  existing event row is confirmed, preventing a second logical count.
- Migration `AddAnalyticsWorkerPersistence` stores the daily pseudonymous visitor fields and adds
  indexes for event ID, link/date timelines, and same-day visitor analytics. The worker requires a
  real SQL Server connection even when the API uses its Development in-memory repository.
- A disposable LocalDB database was migrated through the full migration chain. Processing the same
  event ID twice returned completed both times and produced `logs=1`, `clickCount=1`, with the exact
  UTC event time as `LastAccessedAtUtc`. The disposable database and manual harness were removed; no
  automated test files were added.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors before final
  documentation review.
- Remaining verification: manually force a transient handler failure against a live RabbitMQ
  quorum queue and observe bounded requeue/dead-letter disposition. Docker's local daemon did not
  answer during this session and no Windows RabbitMQ service is installed, so keep this task
  `In Progress` until that broker-backed behavior is observed.
