# TASK-045 — URL, Alias, and Input Security Hardening

**Status:** Planned  
**Phase:** 12 — Security, Privacy & Abuse Hardening

## Goal

Harden destination URL and alias handling against malformed input, route shadowing, resource abuse, and inconsistent normalization while preserving the core purpose of a URL shortener.

## Dependencies

- TASK-044 completed.

## Scope

- Re-review allowed destination schemes and parsing/normalization behavior.
- Define reserved aliases/prefixes for platform routes such as API, auth, health, metrics, Swagger/docs, and Angular application routes where routing could conflict.
- Bound alias/code length and accepted characters.
- Bound URL/header/referrer/user-agent lengths before persistence/queueing.
- Review Unicode/IDN hostname treatment and display risks; document normalization policy.
- Do not add destination fetching/scanning unless an explicit safe-fetch/SSRF design exists.

## Acceptance Criteria

- [ ] Only documented destination schemes are accepted; script/file/unsafe schemes are rejected.
- [ ] Reserved aliases cannot shadow product/API/operations routes.
- [ ] Alias normalization/case-sensitivity matches the database uniqueness model.
- [ ] Oversized destination and metadata inputs are rejected/truncated only according to documented rules before causing oversized DB/queue records.
- [ ] URL parsing relies on robust platform/library parsing rather than ad-hoc regex as the sole authority.
- [ ] IDN/Unicode hostname behavior is documented and displayed safely enough to reduce obvious spoofing confusion.
- [ ] No server-side URL fetch is introduced without SSRF protections and an ADR.
- [ ] Backend/Angular error feedback remains consistent for rejected inputs.
- [ ] Manual boundary-case verification succeeds.
- [ ] No automated test files are added.

## Verification

Exercise reserved aliases, case variants, Unicode/IDN examples, maximum-length inputs, unsupported schemes, malformed URLs, and valid long HTTP/HTTPS URLs.