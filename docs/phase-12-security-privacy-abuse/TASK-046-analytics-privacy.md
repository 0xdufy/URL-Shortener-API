# TASK-046 — Analytics Privacy and Client Identifier Minimization

**Status:** Completed
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

- [x] Raw IP retention has an explicit purpose and bounded duration, or raw IP is removed before long-term analytics persistence.
- [x] Unique-visitor identifier design resists simple reversal/rainbow-table recovery better than an unkeyed raw-IP hash.
- [x] Secret material used for pseudonymous derivation is environment-managed and never logged.
- [x] Referrer storage excludes unnecessary query-string/path detail under the approved analytics model.
- [x] Angular analytics displays only approved aggregate data.
- [x] Privacy changes do not silently change historical/new analytics semantics without documentation.
- [x] Queue and worker event contracts are versioned/migrated if privacy fields change.
- [x] Data inventory and retention decisions are documented.
- [x] Backend/worker/Angular builds succeed.
- [x] No automated test files are added.

## Verification

Trace one redirect from HTTP request through event, worker, persistence, logs, and analytics response and record exactly where identifying fields exist and when they are discarded/transformed.

## Implementation Summary

- Removed unused client-IP collection from authenticated short-URL creation. Redirect handling is
  now the only application path that reads an IP for analytics, and the privacy-aware publisher
  converts it to a UTC-day keyed HMAC before an event is serialized.
- Added `MinimizeAnalyticsClientMetadata`: it removes the legacy `IpAddress` column, conservatively
  clears historical referrer values that are not already host-shaped, renames `Referer` to the
  explicit `ReferrerHost`, and reduces its bound from 512 to 253 characters. New access rows retain
  only bounded user agent, host/kind, and daily pseudonymous identity fields.
- Added validated `RabbitMq:MessageRetentionDays` configuration (default seven days) and applied
  the matching `x-message-ttl` to live and dead-letter quorum queues. The topology migration/recreate
  requirement is documented because RabbitMQ queue arguments are immutable.
- Retained the existing `analytics.click` version-1 wire contract because it was privacy-aware from
  its introduction: raw IP and raw referrer never appeared in it. Documented which future privacy
  representation changes require a new contract version.
- Confirmed the analytics API and Angular page expose aggregate counts/fixed categories only and
  preserve the `sumOfDailyPseudonymousVisitors` method/limitations notice.
- Added [Analytics Privacy and Client-Metadata Lifecycle](../analytics-privacy.md) with the complete
  HTTP, queue, worker, database, Redis, log, API, and UI inventory; discard points; broker/database
  retention; HMAC key management/UTC-boundary rotation; historical cutoff; approximation semantics;
  and the required redirect trace.

## Verification Results

- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors; the
  solution build includes the independently hosted analytics worker.
- `npm.cmd run lint` and `npm.cmd run format:check` completed successfully.
- `cmd /c npm run build` completed successfully. Existing component-style budget warnings remain
  unrelated to this task.
- `dotnet ef migrations script 20260820190159_AddCustomDomainRouting
  20260824093343_MinimizeAnalyticsClientMetadata ... --no-build` completed without applying data.
  The generated transaction sanitizes legacy referrers, drops `IpAddress`, renames the remaining
  host field, and alters it to `nvarchar(253)` in that order.
- Static flow tracing confirmed raw IP/referrer exist only in the live redirect request and
  publisher call; the queue contains only reduced version-1 fields; persistence has no IP property;
  logs use event/link IDs and coarse outcomes; analytics queries/UI use aggregates. The exact trace
  and field lifecycle are recorded in `docs/analytics-privacy.md`.
- Repository searches confirmed no non-migration analytics access-log `IpAddress` property/write
  remains and no automated test file was added.
