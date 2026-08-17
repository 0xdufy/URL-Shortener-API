# TASK-032 — Remove Analytics Writes from Redirect Hot Path

**Status:** Completed

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

- [x] Successful redirect request path performs no synchronous analytics SQL insert/update.
- [x] Redirect target correctness still depends on authoritative link state/cache policy, not asynchronous analytics state.
- [x] Click analytics become eventually consistent with a documented freshness expectation.
- [x] No duplicate synchronous and asynchronous counting remains.
- [x] Queue/publisher failure follows the explicit policy from TASK-030.
- [x] Existing public redirect status semantics remain intact.
- [x] Management UI/API wording does not promise real-time click counts if they are now eventually consistent.
- [x] Build succeeds and SQL tracing/manual inspection confirms analytics writes occur from worker processing, not the redirect HTTP request.
- [x] No automated test files are added.

## Phase 08 Completion Gate

Phase 08 is complete when TASK-029 through TASK-032 are completed and the redirect critical path is read-oriented with analytics persistence handled by the worker.

## Implementation and Verification Notes

- 2026-08-17: Removed `IRedirectAccessRecorder`, `SynchronousRedirectAccessRecorder`, their request
  DTO, dependency-injection registration, and the repository counter/access-log write methods.
  `RedirectResolver` now publishes only after `IsRedirectCurrentAsync` performs an exact read-only
  persisted-state guard, preserving stale-cache rejection and the existing `302`/`404`/`410`
  semantics without consulting asynchronous analytics state.
- `ClickEventPersistence` in the independently hosted worker is now the only code path that updates
  `ClickCount`/`LastAccessedAtUtc` and inserts `ShortUrlAccessLogs`. TASK-030's single confirmed,
  best-effort, fail-open publication attempt remains unchanged, so analytics dependency failure
  cannot mutate or deny an otherwise valid redirect.
- Management API and Angular copy now describe worker-backed click data as eventually consistent.
  Healthy operation is expected to converge within a few seconds; retry, backlog, or outage
  recovery may take longer, and a failed best-effort publication may leave a click unrecorded.
- A disposable LocalDB harness exercised a distributed-cache hit through the real SQL repository.
  SQL command tracing observed one read command and zero analytics writes; immediately afterward
  the database still contained `clickCount=0` and `accessLogs=0`. Passing the captured event to
  `ClickEventPersistence` produced `Persisted`, `clickCount=1`, and `accessLogs=1`. The disposable
  database and manual harness were removed.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. The Angular
  production build also completed; it reported only the existing stylesheet budget warnings for
  three unchanged SCSS files. Static inspection found no remaining legacy recorder or repository
  analytics-write symbols, and no automated test files were added.
