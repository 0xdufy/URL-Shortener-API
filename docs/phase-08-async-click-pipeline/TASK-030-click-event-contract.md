# TASK-030 — Click Event Contract and Redirect Publication Boundary

**Status:** In Progress
**Phase:** 08 — Asynchronous Click Pipeline

## Goal

Define a stable, privacy-aware click event contract and publish it from successful redirects without coupling redirect resolution to analytics persistence.

## Dependencies

- TASK-029 completed.

## Scope

- Define event identifier, short-link identifier, access timestamp, referrer, user-agent metadata, and privacy-approved client identity fields.
- Version the event contract so worker evolution does not require unsafe simultaneous deployment assumptions.
- Publish only after a redirect target is valid/resolvable.
- Define behavior when event publication fails: whether redirect remains available, whether events are buffered/retried, and what loss is acceptable.
- Keep event payload minimal; do not place destination secrets or unnecessary owner data into the queue.

## Acceptance Criteria

- [x] Click event contract has an explicit version and stable event ID.
- [x] Timestamp is UTC and generated at a documented boundary.
- [x] Payload contains only data required for approved analytics.
- [x] Raw IP treatment follows the privacy design or is marked for Phase 12 migration with a bounded interim policy.
- [x] Unknown/inactive/deleted/expired link requests do not emit successful-click events.
- [x] Redirect code depends on a publisher abstraction, not broker SDK types.
- [x] Publication failure semantics are documented and observable enough for later telemetry.
- [x] Event serialization format and compatibility expectations are documented.
- [ ] Backend build succeeds and a manually published event can be inspected.
- [x] No automated test files are added.

## Verification

Perform successful and failed redirect scenarios and inspect emitted events to confirm correct fields, version, timestamp, and no event for rejected redirects.

## Implementation and Verification Notes

- 2026-08-17: Added `analytics.click` contract version 1. Each logical publication attempt receives
  one UUID event ID, also used as the AMQP message ID. The redirect resolver captures UTC access time
  once before cache resolution and uses that same instant in the envelope and payload.
- The payload contains short-link ID, access time, referrer host, bounded user agent, and a keyed
  daily pseudonymous visitor identifier. It excludes destination URL, short code, owner, raw IP,
  full referrer, and request identifiers. Startup requires a Base64 HMAC key of at least 32 bytes;
  the committed Development key is deliberately local-only.
- Publication is invoked through `IRedirectClickEventPublisher` only after the existing
  authoritative access guard accepts a currently active, non-deleted, non-expired link. Transport
  types remain in Infrastructure. TASK-032 still owns removal of the transitional synchronous
  analytics write once TASK-031 provides worker persistence.
- The documented product policy is one bounded, publisher-confirmed attempt with no local buffer or
  ambiguous retry. Transport failure logs event/short-link IDs and fails open so the redirect stays
  available; request cancellation still propagates.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. An ignored,
  temporary manual harness published through the real privacy adapter into a capturing provider and
  inspected camel-case JSON: version/timestamps were correct, user agent was capped at 256,
  referrer reduced to `example.com`, and neither raw IP nor referrer secret appeared. A simulated
  transport exception did not propagate. The harness and its generated files were removed; no test
  files were added.
- Remaining verification: start RabbitMQ and perform live successful/rejected redirect scenarios,
  then inspect the queued message through the broker. Docker Desktop is currently stopped and its
  Windows service could not be started from this environment; TASK-029's live-broker verification
  is likewise still outstanding. Keep this task `In Progress` until that end-to-end path is
  observed.
