# TASK-049 — Retention and Cleanup Jobs

**Status:** Planned  
**Phase:** 13 — Background Jobs & Data Lifecycle

## Goal

Implement explicit retention for deleted links, raw analytics metadata, idempotency records, expired API/session artifacts where owned by this application, and other time-bounded operational data.

## Dependencies

- TASK-048 completed.

## Scope

- Convert Phase 12 privacy/data decisions into configurable retention periods.
- Implement batched cleanup for eligible soft-deleted links and associated dependent data.
- Clean expired idempotency/temporary verification records.
- Clean or transform raw analytics metadata after its approved retention period while preserving allowed aggregates.
- Coordinate cache invalidation when cleanup changes redirectable state.
- Define deletion order and foreign-key behavior deliberately.

## Acceptance Criteria

- [ ] Each cleaned data category has a documented retention rule and owner/business rationale.
- [ ] Cleanup uses bounded batches to avoid one unbounded transaction/table lock.
- [ ] Jobs are safe to rerun after partial failure.
- [ ] Hard deletion respects foreign keys and does not leave orphaned records.
- [ ] Raw analytics privacy retention is enforced without deleting approved long-lived aggregate data accidentally.
- [ ] Deleted-link cleanup cannot remove active/non-eligible links due to timezone/status mistakes.
- [ ] Relevant Redis/cache entries are removed when records become permanently invalid.
- [ ] Cleanup outcomes include counts/duration suitable for later metrics.
- [ ] Manual dry-run/controlled-data verification succeeds.
- [ ] No automated test files are added.

## Verification

Create controlled records on both sides of each retention threshold, run jobs manually, and record exactly which rows/keys were retained and removed.