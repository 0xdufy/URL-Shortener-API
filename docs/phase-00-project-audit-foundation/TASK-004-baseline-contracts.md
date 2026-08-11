# TASK-004 — Baseline API, Data, and Runtime Contracts

**Status:** Planned  
**Phase:** 00 — Project Audit & Engineering Foundation

## Goal

Freeze a documented description of the behavior that exists before modernization so later phases can distinguish intentional breaking changes from accidental regressions.

## Dependencies

- TASK-001 completed.

## Scope

Create `docs/baseline-contracts.md` containing:

- Current routes, methods, request shapes, response shapes, and relevant status codes.
- Current error envelope and validation behavior.
- Current redirect semantics for unknown, inactive, deleted, and expired links.
- Current short-code and custom-alias constraints.
- Current data entities and meaningful indexes/uniqueness constraints.
- Current configuration keys required to run SQL Server and in-memory modes.
- Current cache TTL and invalidation behavior.
- Current rate-limit semantics.
- Current assumptions that are candidates for deliberate change later.

The document is not a promise that all existing behavior is correct. It is a reference point. Mark questionable behavior as `Candidate for change` and link it to the phase expected to address it.

## Acceptance Criteria

- [ ] All public endpoints present at the end of Phase 00 are documented.
- [ ] Error response contract is documented with field names and representative status mappings.
- [ ] Redirect behavior is documented for success, expiration, inactive state, deletion, and missing codes.
- [ ] Alias/code constraints and uniqueness behavior are documented.
- [ ] Current persistence modes and required configuration are documented.
- [ ] In-memory cache and rate-limit limitations are explicitly marked as single-instance assumptions.
- [ ] Known concurrency risk around check-then-insert short-code creation is captured if confirmed by TASK-001.
- [ ] Known synchronous redirect analytics writes are captured if confirmed by TASK-001.
- [ ] Candidate breaking changes point to a future phase instead of being silently implemented here.
- [ ] No product behavior is changed by this task.

## Phase 00 Completion Gate

Phase 00 is complete only when TASK-001 through TASK-004 are `Completed`, the repository builds cleanly under the documented baseline, generated-artifact hygiene is corrected, and the audit/standards/baseline documents exist.