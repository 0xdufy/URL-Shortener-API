# Owner-Scoped Analytics Query API

TASK-034 exposes stable chart and summary models over the active version-2 aggregate schema. Both
routes require a bearer-authenticated user and return `404 NOT_FOUND` when the short code is
unknown, deleted, or owned by another user. This indistinguishable response prevents resource
discovery.

## Routes

- `GET /api/v1/short-urls/{shortCode}/analytics/summary`
- `GET /api/v1/short-urls/{shortCode}/analytics/time-series`

Both routes accept `fromUtc` and `toUtc` as an optional pair. The range always means
`[fromUtc, toUtc)`: `fromUtc` is inclusive and `toUtc` is exclusive. Inputs must explicitly use
`Z` or `+00:00`; local and non-zero-offset timestamps are rejected.

Summary boundaries must be UTC midnight and may span at most 366 days. When omitted, the summary
contains the current UTC day plus the preceding 29 UTC days. `topReferrers` defaults to 10 and is
bounded from 1 through 20. `referrers.otherClicks` preserves the count omitted by the top-N limit.
Device, browser, and operating-system values come from the bounded version-2 classifier and are
returned completely rather than truncated.

Time series supports only `granularity=hour` or `granularity=day`. Hourly boundaries must be whole
UTC hours and are limited to 31 days/744 buckets. Daily boundaries must be UTC midnight and are
limited to 731 buckets. Defaults are the current open bucket plus 23 preceding hourly buckets or
29 preceding daily buckets. Missing aggregate rows are returned as explicit zero-value buckets,
ordered oldest first, so clients do not have to infer chart gaps.

The latest permitted `toUtc` is the end of the current open bucket. This permits current-period
reporting without permitting arbitrary future ranges. A query that includes the current bucket
sets `freshness.includesOpenBucket` and `freshness.isPartial` to `true`.

## Summary model

The summary returns:

- click total from daily `Overall / All` aggregate rows;
- `uniqueVisitorsEstimate`, calculated as the sum of daily pseudonymous unique counts;
- top fixed referrer source labels plus the stable `Direct`, `Other`, and `Unknown` values when
  they rank;
- complete stable device, browser, and OS categories; and
- freshness metadata.

The unique estimate is not a cross-day person count. Daily identity rotation, shared client
addresses, and address changes can overcount or undercount people. No raw IP, user agent, referrer
path/query, pseudonymous visitor identifier, or event-level record is exposed.

## Empty and freshness semantics

An owned link without analytics returns `200` with zero totals, empty breakdown arrays, and a
fully zero-filled time series. `lastAggregatedAtUtc` is `null` until an aggregate exists.

All analytics are eventually consistent because the redirect publishes queue-backed click events
without waiting for worker persistence. `freshness.consistency` is always `eventual`;
`generatedAtUtc` is response-generation time and `lastAggregatedAtUtc` is the latest update time
among matching aggregate rows. A closed range can still lag while a queued or retried event is in
flight, so `isPartial=false` means only that no currently open UTC bucket was requested.

Routine requests query `ShortUrlAnalyticsAggregates` through the indexed
link/granularity/dimension/schema-version/bucket shape. They never expose or scan the raw access-log
table. Referrer ranking is grouped, ordered, and limited in SQL; bucket limits bound response and
query cardinality.
