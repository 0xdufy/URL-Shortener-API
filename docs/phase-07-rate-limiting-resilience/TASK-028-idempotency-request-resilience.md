# TASK-028 — Idempotent Creation and Request Resilience Boundaries

**Status:** Completed
**Phase:** 07 — Distributed Rate Limiting & API Resilience

## Goal

Make retry-prone write operations safer and place explicit bounds around requests/dependency calls without hiding failures behind uncontrolled retries.

## Dependencies

- TASK-027 completed.

## Scope

- Add an idempotency-key contract for URL creation if the audit confirms the workflow benefits from client retries.
- Store idempotency result/state with an expiry and authenticated caller scope.
- Reject reuse of one key with materially different request content.
- Define request body/URL/header size limits relevant to this API.
- Review HTTP/dependency timeout and retry policies; avoid retrying non-idempotent operations blindly.
- Ensure cancellation propagates to database/Redis operations where supported.

## Acceptance Criteria

- [x] Repeating the same accepted create request with the same valid idempotency key returns the documented same logical result instead of creating duplicates.
- [x] An idempotency key is scoped so User A cannot obtain User B's stored result.
- [x] Reusing a key with a different payload returns a documented conflict/validation outcome.
- [x] Idempotency records expire and cannot grow without bound.
- [x] Request size limits protect obviously unreasonable input while allowing valid destination URLs within documented constraints.
- [x] Dependency retries are bounded and applied only where safe.
- [x] Request cancellation reaches I/O work where practical.
- [x] Error responses remain consistent and do not leak internal dependency details.
- [x] Manual retry/cancellation scenarios are documented.
- [x] No automated test files are added.

## Phase 07 Completion Gate

Phase 07 is complete when TASK-026 through TASK-028 are completed and multi-instance rate limiting, proxy trust, and write-retry semantics are explicit and manually verified.

## Completion Notes

- Added optional, validated `Idempotency-Key` handling for authenticated URL creation. Atomic SQL
  persistence, hashed keys/payloads, a caller-scoped unique index, and a short-link reference make
  sequential and concurrent retries return one logical result. Different content returns the common
  `409 IDEMPOTENCY_KEY_REUSED` response.
- Added configurable 1-168 hour retention (24 hours by default), an expiry index, and opportunistic
  expired-record deletion on keyed creation. The Development in-memory repository mirrors the
  contract inside one lock while remaining explicitly unsuitable for multi-instance verification.
- Added startup-validated Kestrel body/request-line/header/count/header-time bounds, an 8 KiB create
  body limit, a 15-second request execution timeout, and common `413`/`504` envelopes. Request and
  timeout cancellation continues through EF, Redis waits, caching, validation, and application calls
  where supported.
- Removed the global EF transient execution retry, which could replay ambiguous non-idempotent
  writes. SQL commands remain timeout-bounded; Redis connection retries remain bounded, while Redis
  operations are not blindly replayed.
- On 2026-08-17, a disposable LocalDB database demonstrated sequential replay (`201,201`, same ID
  and code), concurrent custom-alias replay (`201,201`, same ID), different-content conflict
  (`409 IDEMPOTENCY_KEY_REUSED`), and identical key text for another user creating an isolated ID.
  Stored rows showed exactly 24-hour retention; forcing one disposable row past expiry caused cleanup
  and a new generated result. Invalid keys returned `400`; a 9,050-byte create returned the common
  `413 REQUEST_TOO_LARGE` envelope. A deliberately unavailable LocalDB connection exceeded 15
  seconds, returned `504 REQUEST_TIMEOUT`, and showed cancellation inside EF connection opening.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. No automated
  test files were added.
