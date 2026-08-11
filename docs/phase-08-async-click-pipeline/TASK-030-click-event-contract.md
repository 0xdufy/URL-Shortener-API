# TASK-030 — Click Event Contract and Redirect Publication Boundary

**Status:** Planned  
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

- [ ] Click event contract has an explicit version and stable event ID.
- [ ] Timestamp is UTC and generated at a documented boundary.
- [ ] Payload contains only data required for approved analytics.
- [ ] Raw IP treatment follows the privacy design or is marked for Phase 12 migration with a bounded interim policy.
- [ ] Unknown/inactive/deleted/expired link requests do not emit successful-click events.
- [ ] Redirect code depends on a publisher abstraction, not broker SDK types.
- [ ] Publication failure semantics are documented and observable enough for later telemetry.
- [ ] Event serialization format and compatibility expectations are documented.
- [ ] Backend build succeeds and a manually published event can be inspected.
- [ ] No automated test files are added.

## Verification

Perform successful and failed redirect scenarios and inspect emitted events to confirm correct fields, version, timestamp, and no event for rejected redirects.