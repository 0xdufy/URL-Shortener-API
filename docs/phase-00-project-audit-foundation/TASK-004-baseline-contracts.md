# TASK-004 — Baseline API, Data, and Runtime Contracts

**Status:** Completed  
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

- [x] All public endpoints present at the end of Phase 00 are documented.
- [x] Error response contract is documented with field names and representative status mappings.
- [x] Redirect behavior is documented for success, expiration, inactive state, deletion, and missing codes.
- [x] Alias/code constraints and uniqueness behavior are documented.
- [x] Current persistence modes and required configuration are documented.
- [x] In-memory cache and rate-limit limitations are explicitly marked as single-instance assumptions.
- [x] Known concurrency risk around check-then-insert short-code creation is captured if confirmed by TASK-001.
- [x] Known synchronous redirect analytics writes are captured if confirmed by TASK-001.
- [x] Candidate breaking changes point to a future phase instead of being silently implemented here.
- [x] No product behavior is changed by this task.

## Phase 00 Completion Gate

Phase 00 is complete only when TASK-001 through TASK-004 are `Completed`, the repository builds cleanly under the documented baseline, generated-artifact hygiene is corrected, and the audit/standards/baseline documents exist.

## Implementation and Verification Record

- Completed 2026-08-11.
- Added `docs/baseline-contracts.md` covering all six routes, shapes, status/error behavior, redirect states, validation/code constraints, entities/indexes, runtime configuration, cache/rate-limit semantics, and future change ownership.
- Confirmed the mixed middleware/controller JSON casing from both source inspection and a Development in-memory smoke run.
- Smoke verification observed `201` plus relative `Location`, `200` details, `302` plus destination `Location`, `404` controller envelope, and `400` middleware validation envelope.
- No product behavior was changed.
