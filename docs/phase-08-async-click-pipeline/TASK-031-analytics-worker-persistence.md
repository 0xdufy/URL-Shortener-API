# TASK-031 — Analytics Worker, Idempotency, and Persistence

**Status:** Planned  
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

- [ ] Re-delivery of the same event does not increment analytics more than once.
- [ ] Database uniqueness/transaction rules support the idempotency guarantee.
- [ ] Worker failures do not acknowledge/drop an event contrary to the selected delivery semantics.
- [ ] Poison/failing messages have a bounded retry path and visible disposition.
- [ ] Worker handles graceful shutdown without intentionally abandoning acknowledged-but-unpersisted work.
- [ ] Click count/last-access metadata has one documented source of truth.
- [ ] Worker does not require HTTP controller context.
- [ ] Database indexes support expected event/aggregate queries.
- [ ] Worker build and manual duplicate-event verification succeed.
- [ ] No automated test files are added.

## Verification

Publish the same event ID more than once, verify only one logical click is persisted/counted, and manually exercise transient consumer failure/retry behavior.