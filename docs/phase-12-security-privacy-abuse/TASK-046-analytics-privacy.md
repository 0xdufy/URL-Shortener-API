# TASK-046 — Analytics Privacy and Client Identifier Minimization

**Status:** Planned  
**Phase:** 12 — Security, Privacy & Abuse Hardening

## Goal

Minimize collection and retention of client-identifying data while preserving the aggregate analytics capabilities approved in Phase 09.

## Dependencies

- TASK-045 completed.

## Scope

- Inventory IP, user-agent, referrer, and other client metadata across HTTP handling, queue events, raw storage, aggregates, logs, and dashboards.
- Replace long-term raw IP storage with an approved privacy-preserving visitor identifier where unique-visitor estimates require one.
- Prefer keyed HMAC/pseudonymous derivation with documented key rotation/retention boundaries over plain unsalted hashing when appropriate.
- Define retention periods for raw/high-cardinality metadata.
- Ensure referrer normalization removes unnecessary path/query information unless explicitly required.
- Document what analytics are approximate and why.

## Acceptance Criteria

- [ ] Raw IP retention has an explicit purpose and bounded duration, or raw IP is removed before long-term analytics persistence.
- [ ] Unique-visitor identifier design resists simple reversal/rainbow-table recovery better than an unkeyed raw-IP hash.
- [ ] Secret material used for pseudonymous derivation is environment-managed and never logged.
- [ ] Referrer storage excludes unnecessary query-string/path detail under the approved analytics model.
- [ ] Angular analytics displays only approved aggregate data.
- [ ] Privacy changes do not silently change historical/new analytics semantics without documentation.
- [ ] Queue and worker event contracts are versioned/migrated if privacy fields change.
- [ ] Data inventory and retention decisions are documented.
- [ ] Backend/worker/Angular builds succeed.
- [ ] No automated test files are added.

## Verification

Trace one redirect from HTTP request through event, worker, persistence, logs, and analytics response and record exactly where identifying fields exist and when they are discarded/transformed.