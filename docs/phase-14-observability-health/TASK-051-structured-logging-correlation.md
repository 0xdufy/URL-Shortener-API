# TASK-051 — Structured Logging and Correlation

**Status:** Planned  
**Phase:** 14 — Observability & Operational Health

## Goal

Standardize structured logs across API and workers so requests, click events, jobs, and failures can be correlated without leaking credentials or sensitive analytics data.

## Dependencies

- Phase 13 completed.

## Scope

- Define structured logging fields and event naming conventions.
- Ensure HTTP trace/correlation IDs are returned in error contracts and present in request logs.
- Propagate correlation/event identifiers through click publication/worker processing where useful.
- Add scoped fields for job name/run ID and dependency failures.
- Review log levels to avoid high-volume success logging on redirect hot path.
- Apply Phase 12 redaction/privacy requirements.

## Acceptance Criteria

- [ ] API requests have a trace/correlation identifier usable from client error to server logs.
- [ ] Worker processing logs include stable event ID and trace linkage where available without storing secret payloads.
- [ ] Maintenance jobs include job/run identifiers and outcome counts.
- [ ] Passwords, cookies/session tokens, Authorization headers, API-key secrets, verification tokens, and raw privacy-sensitive fields are not logged.
- [ ] High-frequency redirect success logging is sampled/leveled or otherwise bounded according to the observability design.
- [ ] Exceptions retain useful stack/context internally while public responses remain sanitized.
- [ ] Logging configuration is environment-aware.
- [ ] API/worker builds and manual correlation walkthrough succeed.
- [ ] No automated test files are added.

## Verification

Follow one management request, one redirect/click event, one worker failure/retry, and one maintenance job through logs and record which identifiers link the operations.