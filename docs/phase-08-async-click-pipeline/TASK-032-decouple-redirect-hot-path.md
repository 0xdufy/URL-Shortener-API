# TASK-032 — Remove Analytics Writes from Redirect Hot Path

**Status:** Planned  
**Phase:** 08 — Asynchronous Click Pipeline

## Goal

Complete the architectural transition so a successful redirect no longer waits for SQL click-count/access-log persistence.

## Dependencies

- TASK-031 completed.

## Scope

- Remove synchronous analytics database writes from redirect request handling.
- Resolve target from distributed cache/database, publish the click event according to TASK-030 policy, and return redirect without waiting for analytics persistence.
- Remove or repurpose old synchronous access-registration methods so there is one analytics path.
- Document expected eventual-consistency delay for management statistics.
- Ensure an analytics dependency failure cannot corrupt redirect state.

## Acceptance Criteria

- [ ] Successful redirect request path performs no synchronous analytics SQL insert/update.
- [ ] Redirect target correctness still depends on authoritative link state/cache policy, not asynchronous analytics state.
- [ ] Click analytics become eventually consistent with a documented freshness expectation.
- [ ] No duplicate synchronous and asynchronous counting remains.
- [ ] Queue/publisher failure follows the explicit policy from TASK-030.
- [ ] Existing public redirect status semantics remain intact.
- [ ] Management UI/API wording does not promise real-time click counts if they are now eventually consistent.
- [ ] Build succeeds and SQL tracing/manual inspection confirms analytics writes occur from worker processing, not the redirect HTTP request.
- [ ] No automated test files are added.

## Phase 08 Completion Gate

Phase 08 is complete when TASK-029 through TASK-032 are completed and the redirect critical path is read-oriented with analytics persistence handled by the worker.