# TASK-045 — URL, Alias, and Input Security Hardening

**Status:** Completed
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

- [x] Only documented destination schemes are accepted; script/file/unsafe schemes are rejected.
- [x] Reserved aliases cannot shadow product/API/operations routes.
- [x] Alias normalization/case-sensitivity matches the database uniqueness model.
- [x] Oversized destination and metadata inputs are rejected/truncated only according to documented rules before causing oversized DB/queue records.
- [x] URL parsing relies on robust platform/library parsing rather than ad-hoc regex as the sole authority.
- [x] IDN/Unicode hostname behavior is documented and displayed safely enough to reduce obvious spoofing confusion.
- [x] No server-side URL fetch is introduced without SSRF protections and an ADR.
- [x] Backend/Angular error feedback remains consistent for rejected inputs.
- [x] Manual boundary-case verification succeeds.
- [x] No automated test files are added.

## Verification

Exercise reserved aliases, case variants, Unicode/IDN examples, maximum-length inputs, unsupported schemes, malformed URLs, and valid long HTTP/HTTPS URLs.

## Implementation Summary

- Added one application input policy for the 2,048-character destination bound, robust .NET URI
  parsing, HTTP(S) allow-list, credential/control/whitespace rejection, canonical storage, IDN
  Punycode conversion, 4–20 character ASCII short-code shape, and case-insensitive reserved-route
  detection.
- Applied canonicalization before idempotency hashing and persistence on create/update. Invalid
  short-code lookups now stop before cache/database access, and generated candidates also avoid
  reserved routes.
- Reserved platform/API/auth/operations/documentation/Angular roots, including separator-prefixed
  namespaces, while retaining case-preserving, case-sensitive alias uniqueness to match SQL Server
  and in-memory persistence.
- Bounded raw referrer processing at 2,048 characters, retained the 253-character normalized IDN
  host contract, and centralized the existing 256-character user-agent producer/consumer bound.
- Mirrored URL credentials/normalized-length and reserved-alias checks in Angular while retaining
  authoritative backend field details through the existing error envelope.
- Documented canonicalization, safe Punycode display, metadata handling, and the explicit no-fetch
  SSRF boundary in
  [URL, Alias, and Redirect-Metadata Security](../url-alias-input-security.md).

## Verification Results

- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors.
- `npm.cmd run build` completed successfully. Existing component-style budget warnings remain
  unrelated to this task.
- `npm.cmd run lint` and `npm.cmd run format:check` completed successfully.
- A temporary PowerShell reflection harness (no file added) exercised the compiled policy and
  validators. Exact 2,048-character HTTP input was accepted; 2,049 characters, relative/malformed
  values, surrounding whitespace, embedded credentials, `javascript:`, `data:`, and `file:` were
  rejected. A long valid HTTPS destination normalized successfully.
- Unicode `https://例え.テスト/a` normalized to
  `https://xn--r8jz45g.xn--zckzah/a`; URL scheme/host casing, escaping, and a default HTTPS port
  normalized consistently.
- `Docs`, `API_v1`, and `app-settings` were rejected as reserved across case variants;
  `application`, `rapid`, and valid mixed-case `Launch` remained available. Invalid characters and
  21-character codes failed the shape check while a 20-character code passed.
- No automated test file or server-side destination fetch was added.
