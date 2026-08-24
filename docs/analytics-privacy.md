# Analytics Privacy and Client-Metadata Lifecycle

This document is the privacy boundary for redirect analytics introduced by TASK-046. The product
supports aggregate click trends, coarse referrer sources, coarse user-agent classifications, and
an approximate sum of daily unique visitors. It does not support event export, person tracking,
cross-day identity, precise referrer URLs, destination/campaign inspection, or geography.

## Data inventory

| Stage | Client-derived data | Form and purpose | Discard/retention boundary |
| --- | --- | --- | --- |
| HTTP redirect | Effective client IP, `User-Agent`, `Referer` | Request-memory inputs for daily uniqueness and coarse dimensions | Raw values leave request scope only through the transformations below; they are never written by the controller or resolver. |
| Click publisher | Effective IP, bounded user agent, parsed referrer | IP is immediately converted to a daily keyed HMAC identifier. An HTTP(S) referrer is reduced to lower-case IDN ASCII host plus `host`; missing is `direct`; unsafe or invalid input becomes `unknown`. | The raw IP and raw referrer are discarded before serialization. |
| RabbitMQ click event v1 | Short-link ID, event/time IDs, host/kind, user agent (maximum 256), daily pseudonymous ID/period/scheme | Durable handoff to the analytics worker | Main and dead-letter quorum queues each expire messages after `RabbitMq:MessageRetentionDays` (default seven days). A message dead-lettered only as the main-queue TTL expires can therefore exist for less than twice that setting. |
| Worker | Same privacy-reduced event fields | Deterministic classification and transactional aggregation | Values exist only during delivery handling, except for the persistence rows below. Logs contain event ID, short-link ID, contract version, status, and errors—not event payload fields. |
| Raw analytics row | Event/time/link IDs, bounded user agent, referrer host/kind, daily pseudonymous ID/period/scheme | Idempotency, bounded correction/reconciliation, and diagnostic source of truth | 30-day target, enforced by TASK-049. `IpAddress` no longer exists. `ReferrerHost` is capped at 253; no path, query, fragment, credentials, or port is stored. |
| Daily visitor key | Link ID, UTC day, pseudonymous ID, scheme, first-seen time | Concurrency-safe daily unique estimate | 25 months, matching mutable daily aggregates; TASK-049 owns enforcement. |
| Aggregates | UTC bucket, fixed dimension/version/value, click and unique counts | Owner-facing analytics queries | Hourly rows: 90 days. Daily rows: 25 months. TASK-049 owns enforcement. No client string or pseudonymous ID is present. |
| Redis rate limiting | Effective IP for anonymous/auth policies | SHA-256 partition in a short-lived limiter key; this is security/availability state, not analytics | Fixed/sliding keys expire within bounded policy windows; token buckets expire after bounded refill. Raw IP is not present in the key. |
| API/worker logs | Route, status, duration, event ID, short-link ID, coarse outcome | Operations and retry diagnosis | No IP, user agent, referrer, pseudonymous visitor ID, event body, destination URL, header, or HMAC key is intentionally logged. Log-store retention is an operational policy and must not be used to retain prohibited fields. |
| Analytics API and Angular | Aggregate counts, fixed categories, UTC buckets, freshness, method label | Owned-link reporting | Responses and UI expose no event row, IP, user agent, referrer host/path/query, or pseudonymous ID. |

The URL-create endpoint formerly accepted an unused client-IP argument. TASK-046 removes that
collection path; URL creation, idempotency, and ownership do not require an IP address.

## Daily pseudonymous visitor design

The producer computes base64url `HMAC-SHA-256(key, yyyy-MM-dd + "\n" + normalized-effective-IP)`.
The key is at least 32 random bytes, is shared by API replicas in one environment, and is supplied
only through `ClickEvents:VisitorIdentityHmacKeyBase64` (normally
`ClickEvents__VisitorIdentityHmacKeyBase64`) from the deployment secret manager. Base settings do
not contain a production key; startup validation rejects a missing/short key. The visible
Development value is local-only. No code path logs configuration or the key.

The keyed construction prevents precomputed/rainbow-table recovery available against an unkeyed
IP hash. It is still a pseudonym, not anonymization: an operator holding the key can test candidate
addresses. Access to the key and visitor tables must therefore remain restricted.

Rotate the key at a UTC day boundary during a coordinated API rollout. Preserve one key across all
replicas until the rollout is complete; a mixed or mid-day rollout splits that day's identity
population and can overcount uniques. Old keys are not needed to consume already-produced events
or query aggregates and should be retired from the secret manager after the old producer set has
stopped. Use distinct keys per environment. A deliberate key rotation begins a new identity
population; record its UTC effective date in the deployment log. The identity scheme remains
`hmac-sha256-utc-day-v1` because rotation changes secret material, not the wire algorithm.

## Historical and contract semantics

Migration `MinimizeAnalyticsClientMetadata` permanently drops the legacy raw-IP column. It keeps
only pre-existing `Referer` values that already match a conservative ASCII host shape, lower-cases
them, renames the column to `ReferrerHost`, and reduces its maximum length from 512 to 253. Other
legacy referrer strings are cleared. Existing aggregates, click totals, user-agent values, and
pseudonymous IDs are unchanged. Consequently historical aggregate dashboards retain their prior
meaning, while a future raw-event rebuild cannot recover a cleared legacy referrer breakdown.

The wire event was privacy-aware from `analytics.click` contract version 1: it never included raw
IP or raw referrer and already carried the daily HMAC fields. TASK-046 therefore requires no queue
contract version change or mixed-version consumer. Adding a raw field, changing the HMAC input or
period, or changing referrer representation requires a new contract version and an explicit
deployment/migration plan. Fixed source or user-agent classification changes follow the separate
aggregate `DimensionSchemaVersion` rule.

The unique value shown by the API and Angular is `sumOfDailyPseudonymousVisitors`. It is not a
person count: shared addresses undercount, address changes and key rotation can overcount, daily
rotation counts one visitor again on another UTC day, and fail-open publication can omit clicks.
These limitations are part of the response method label and the Angular privacy note, not a silent
change to exact unique-person semantics.

## Redirect trace

For one successful `GET /r/{shortCode}`:

1. `RedirectController` reads the effective normalized IP, user agent, and referrer from the live
   request. Its completion log includes only short code, outcome/source, and duration.
2. `RedirectResolver` passes these values in memory only after link state is confirmed. No
   analytics SQL write occurs in the request path.
3. `PrivacyAwareRedirectClickEventPublisher` derives the daily HMAC, caps the user agent, and
   converts the referrer to host/kind before JSON serialization. Raw IP and raw referrer end here.
4. RabbitMQ stores only the version-1 reduced payload under bounded queue TTLs. Publisher failure
   logs event and short-link IDs only and remains fail-open for redirect availability.
5. `AnalyticsWorkerService` validates version 1. `ClickEventPersistence` classifies coarse
   dimensions, inserts the daily uniqueness key if new, updates rollups, and inserts the bounded
   raw row in one SQL transaction. Worker logs contain event and short-link IDs only.
6. Owner analytics queries read `ShortUrlAnalyticsAggregates`, never client metadata. The response
   contains counts, fixed categories, buckets, freshness, and the unique-estimate method. Angular
   renders only those approved aggregate fields and explains the estimate's limitations.

Operational verification should inspect a captured event and database row only in an isolated
environment, confirm `IpAddress` is absent and referrer input such as
`https://Example.com/private/path?token=value` becomes only `example.com`, and remove the
verification data afterward.
