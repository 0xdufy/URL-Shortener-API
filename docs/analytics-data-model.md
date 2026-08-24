# Analytics Data Model and Aggregation

TASK-033 introduces versioned, query-oriented click rollups. The access-log event remains the
recoverable event-level source of truth, while owner-facing analytics read aggregate rows. This
separates routine dashboard cost from the number of raw events retained for correction or audit.

## Supported analytics

All bucket boundaries use UTC and the event's `AccessedAtUtc`, never worker processing time.

| Insight | Stored granularity | Aggregate dimension/value |
| --- | --- | --- |
| Total click trend | Hour and day | `Overall` / `All` |
| Referrer/source clicks | Day | `Referrer` / fixed source label, `Direct`, `Other`, or `Unknown` |
| Device-class clicks | Day | `Device` / `Desktop`, `Mobile`, `Tablet`, `Bot`, `Other`, or `Unknown` |
| Browser-family clicks | Day | `Browser` / `Edge`, `Opera`, `Chrome`, `Firefox`, `Safari`, `Internet Explorer`, `Other`, or `Unknown` |
| OS-family clicks | Day | `OperatingSystem` / `Windows`, `Android`, `iOS`, `macOS`, `Linux`, `Other`, or `Unknown` |
| Unique-visitor estimate | Day only | `Overall` / `All`, using the event's daily pseudonymous identifier |

Hourly dimension breakdowns, cross-dimension combinations, cross-day visitor identity, raw event
exports, referrer paths/queries, destination analysis, campaign attribution, and geographic
analytics are unsupported. Geography requires a separate approved provider, accuracy, licensing,
and privacy decision; no location is inferred from referrer, user agent, or pseudonymous visitor
data.

## Storage and processing flow

For every accepted click event, the analytics worker uses one SQL transaction to:

1. update the link's `ClickCount` and monotonic `LastAccessedAtUtc` projection;
2. reserve the `(ShortUrlId, UTC date, PseudonymousVisitorId)` daily visitor key if it is new;
3. increment the hourly overall row, daily overall row, and four daily dimension rows; and
4. insert `ShortUrlAccessLogs` with the integration-event ID as its idempotency primary key.

The raw access row contains bounded user agent, normalized `ReferrerHost` and kind, daily
pseudonymous visitor ID, and identity scheme. It contains no raw IP. Dimension classification
occurs in the worker immediately before transactional aggregation. Raw strings are never used as
browser, OS, device, or displayed referrer keys. Referrer hosts are lower-cased IDN ASCII host
names capped at 253 characters; paths, queries, fragments, user information, and ports never enter
the event or access row.

The schema migration backfills hourly/daily overall rows, daily dimensions, and available daily
visitor keys from pre-existing access logs. Legacy records without a pseudonymous visitor ID
contribute clicks but cannot contribute a unique estimate.

The aggregate primary key is:

`(ShortUrlId, Granularity, Dimension, DimensionSchemaVersion, BucketStartUtc, DimensionValue)`.

This database-enforced key prevents multiple rows for the same logical bucket and dimension. The
same leading columns support link/range/dimension reads; `IX_AnalyticsAggregates_Link_Query` makes
that query shape explicit. Aggregate updates take serializable key-range update locks, so
concurrent first clicks cannot create duplicate rows.

`ShortUrlAnalyticsDailyVisitors` uses
`(ShortUrlId, IdentityPeriodUtc, PseudonymousVisitorId)` as its primary key. The range-protected
insert makes daily unique increments concurrency-safe. Its period/link index supports bounded
retention cleanup. Only daily overall rows carry `UniqueVisitorCount`; dimension-level unique
visitors are not supported.

The access-log event ID remains the final retry/idempotency boundary. If its insert conflicts, the
transaction rolls back link, visitor, and aggregate changes before the worker treats the delivery
as complete. Therefore a retried event cannot increment a rollup twice.

## Categorization and schema versions

Dimension schema version `2` is the active enrichment schema. It uses a repository-owned fixed
signature table rather than an external user-agent package or a remotely updated parser database.
That choice keeps classification deterministic for the deployed code version and avoids parser
updates silently rewriting dashboard meaning.

The user-agent taxonomy is deliberately bounded:

- devices: `Desktop`, `Mobile`, `Tablet`, `Bot`, `Other`, and `Unknown`;
- browsers: `Edge`, `Opera`, `Chrome`, `Firefox`, `Safari`, `Internet Explorer`, `Other`, and
  `Unknown`; and
- operating systems: `Windows`, `Android`, `iOS`, `macOS`, `Linux`, `Other`, and `Unknown`.

Signature order is part of the schema. Bot signatures are evaluated before device signatures;
tablet and mobile signatures precede desktop signatures. Edge and Opera precede Chrome because
their user agents also contain Chrome-compatible tokens. iOS precedes macOS/Linux for the same
reason. Missing, empty, or control-character-containing user agents yield `Unknown`; a syntactically
usable but unsupported value yields `Other`. No parsing outcome throws or becomes a raw dimension
key. The publisher trims and caps a user agent at 256 characters. A producer that bypasses the
publisher and sends an oversized contract field is permanently rejected rather than stored.

Referrer kind distinguishes a genuinely missing header (`Direct`) from a present but malformed,
non-HTTP(S), or oversized value (`Unknown`). Valid hosts map by exact domain or subdomain boundary
to this fixed source set: `Google`, `Bing`, `DuckDuckGo`, `Yahoo`, `Facebook`, `Instagram`,
`X / Twitter`, `LinkedIn`, `Reddit`, and `YouTube`. Every other valid host maps to `Other`, so
attacker-controlled host names cannot create unbounded dashboard labels. The optional kind field
is backward-compatible with queued version-1 click events: when absent, a missing host is inferred
as direct and a present host is classified normally.

Any signature, source-domain, precedence, or output-label change that remaps a value must increment
`DimensionSchemaVersion`, include a data migration or explicit history cutoff, and cut queries over
without combining versions. An external parser adopted later must be version-pinned; its data-file
version and update procedure become part of the schema decision. Migration
`AddAnalyticsEnrichmentV2` preserves version-1 totals and UA dimensions, coalesces historical host
rows into version-2 source labels, and retains version-1 rows for an immediate rollback.

For deployment, stop the analytics worker, apply the migration, deploy the API and worker, and then
restart consumption. This prevents a version-1 worker from writing rows after the version-2
backfill. A rollback after version-2 events have been consumed requires rebuilding the affected
version-1 interval from retained access logs before switching queries back; simply running the
down migration would discard the version-2 projection for that interval.

Geographic analytics remain unsupported. No country, region, or city is inferred or claimed. A
future implementation requires a separate ADR covering provider, accuracy, database updates,
privacy, retention, and licensing before a geographic field or dashboard label is introduced.

## Query and consistency rules

The owner-facing route contract, UTC range rules, cardinality limits, empty response, and freshness
model are documented in `docs/analytics-query-api.md`.

Routine dashboards query `ShortUrlAnalyticsAggregates`; they do not group or count an unbounded
`ShortUrlAccessLogs` range. Hourly rows serve short-range trends. Daily rows serve longer trends,
breakdowns, and daily unique estimates. A range spanning partial days may use hourly overall rows
for click totals, but dimension and unique results remain daily and must be labeled as such by the
query API.

The pre-existing `/stats` contract preserves exact arbitrary timestamp bounds by summing hourly
overall rows for complete hours and scanning raw events only for the at-most-two partial boundary
hours. A range shorter than one hour is inherently bounded and reads that raw slice directly. It
never scans the raw table across the full requested history.

Aggregates are eventually consistent because redirect publication is fail-open and queue-backed.
The estimate can undercount when publication fails, and daily key rotation, shared addresses, and
address changes mean it is not a person-level count. It deliberately cannot track a visitor across
UTC dates and does not require retaining raw IP.

Late and out-of-order valid events update the bucket determined by their original event timestamp.
They also advance `UpdatedAtUtc` on the affected rows, while `LastAccessedAtUtc` never moves
backward. An event-ID retry remains a no-op. A distinct late event with an already-seen same-day
visitor increments clicks but not daily uniques.

## Retention implications

These are the database data-lifecycle targets for TASK-049 to enforce with restart-safe jobs:

- raw access logs, including bounded user agent and pseudonymous visitor ID: 30 days;
- hourly aggregates: 90 days;
- daily aggregates and matching daily visitor keys: 25 months.

Daily visitor keys must live as long as their daily aggregates remain mutable; deleting them first
would allow a late click to double-count a visitor. Cleanup must use a shared UTC cutoff and remove
visitor keys with the matching daily bucket. Once automated retention exists, ingestion and
cleanup must share a closed-bucket policy so very old late events cannot resurrect expired rows.
Until TASK-049 implements enforcement, these are explicit operational targets rather than an
automatic deletion claim.

Deleting a short URL cascades its raw events, rollups, and visitor keys. Soft deletion does not,
which preserves owner analytics through the restore window.
