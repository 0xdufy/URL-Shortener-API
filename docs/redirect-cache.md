# Distributed Redirect Cache

## Topology and Key Contract

Redirect lookups use the application `IShortUrlCache` boundary backed directly by the shared
Redis `IDistributedCache` provider. There is no process-local L1 cache, so healthy API instances
read and invalidate the same entry. The provider-level namespace from `Redis:KeyPrefix` is
followed by this feature-owned key:

```text
redirect:v1:<case-sensitive-short-code>
```

For example, `myAlias_01` in Development is stored under the physical key
`url-shortener:development:v1:redirect:v1:myAlias_01`. The feature version changes when an
incompatible redirect payload or key layout is introduced.

## Cached Data and Serialization

Values are compact UTF-8 JSON with camel-case property names. Version 1 contains only:

- `schemaVersion`
- `shortUrlId`
- `originalUrl`
- `expiresAtUtc`

The ID and redirect fields are required to resolve and safely validate a redirect. Ownership,
deletion metadata, click counts, access data, and all identity/management-only fields are not
cached. Invalid JSON, an unsupported schema version, an empty ID, or a destination that is not an
absolute HTTP/HTTPS URL is treated as a miss and evicted on a best-effort basis.

Only active, non-deleted, non-expired links receive positive entries. Unknown, deleted, inactive,
and expired codes are not negative-cached, so arbitrary misses cannot create persistent cache
entries.

## Resolution Contract

The anonymous `GET /r/{shortCode}` route depends on the dedicated `IRedirectResolver` application
boundary. It does not resolve a current user, owner, bearer token, or management resource. The
resolver returns an explicit status and source instead of leaking HTTP status-code tuples into the
application layer; the API maps those statuses to the public contract:

| Persisted state | Resolution status | HTTP result |
| --- | --- | --- |
| Active, not deleted, and `ExpiresAtUtc` is null or later than request UTC | `Resolved` | `302 Found` |
| Unknown, deleted, or inactive | `NotFound` | `404 NOT_FOUND` |
| Active, not deleted, and `ExpiresAtUtc <= request UTC` | `Expired` | `410 EXPIRED` |

Deletion/inactivity takes precedence over expiry, so a deleted or inactive expired row remains a
concealed `404`. A single state evaluator classifies both cached and persisted candidates. Because
version 1 cache values are positive-only and deliberately omit management state, a cache hit is
still authorized by the persisted active/deleted/expiry guard before its destination is returned.
An expired cached candidate is evicted and reloaded instead of directly returning `410`, allowing
a concurrently extended expiry to resolve from authoritative persistence.

If the persisted row changes between fallback lookup and the atomic guard, the resolver reloads
and reclassifies it. This retry is bounded to three persistence attempts. The common concurrent
inactive/deleted/expired/destination-change cases therefore return their current documented result
or the new valid destination; sustained mutation churn fails closed as `404` rather than returning
an unverified destination.

Persistence projects only short URL ID/code, destination, expiry, active state, and deletion state.
Owner identity, deletion timestamps, counters, and other management data are not materialized by
the redirect lookup.

## Expiration Policy

Every entry uses an absolute UTC expiration:

```text
min(link expiry, cache-fill time + 24 hours)
```

A non-expiring link therefore has a 24-hour cache lifetime and is repopulated on demand. An
expiring link never intentionally has a physical cache expiration later than its link expiry. If
the computed absolute expiration is already past, no entry is written. Expired cached values are
evicted and resolved from persistence rather than being trusted; this also handles a concurrently
extended link expiry.

## Invalidation and Race Safety

Destination, expiry, active status, soft deletion, and restore changes invalidate the short-code
key immediately after the persistence commit. Aliases are immutable in the current management
contract. Any future alias mutation must invalidate both the old and new short-code keys after its
uniqueness-safe commit.

Invalidation alone does not close a cache-aside fill race, and a Redis outage can prevent a remove
from reaching the server. Before returning a cached destination, the existing atomic click update
also requires all of the following persisted state to match:

- the cached short URL ID;
- the destination using an exact binary comparison;
- the exact nullable expiry;
- active and not deleted state;
- expiry later than the access time, when present.

If that guard updates no row, the API evicts the entry, reloads by short code without EF tracking,
and applies the same state evaluator. Consequently, a stale entry cannot authorize a redirect
after a destination, expiry, status, deletion, or restore mutation, even when invalidation failed
or raced with a fill. A successful mutation is visible to another healthy instance on its next
redirect lookup; an already in-flight redirect is ordered by the persisted guard it completed
against.

## Access Recording and Click Publication Boundary

Phase 06 introduced and TASK-030 still preserves synchronous click-count and access-log persistence
until the worker persistence path is ready. The resolver
calls the application-level `IRedirectAccessRecorder` with the validated short URL ID, exact
destination/expiry snapshot, access UTC, and existing client metadata. Its current implementation
performs the atomic state guard/counter update and access-log insert. Only after that guard succeeds,
the resolver calls the broker-neutral `IRedirectClickEventPublisher`. It reduces client metadata to
the privacy boundary documented in `click-event-transport.md` and makes a bounded best-effort queue
publication. Publication failure is observable but fail-open, while stale/invalid link rejection
still evicts cache and emits no successful-click event. TASK-032 removes the transitional
synchronous analytics recorder after TASK-031 supplies the worker persistence boundary.

The controller emits a debug-level structured completion log containing short code, resolution
status, cache/persistence source, and elapsed milliseconds. It does not log the destination or
client metadata, and it does not introduce Phase 14 metrics or tracing.

## Redis Failure Behavior

Redis connection and operation timeouts remain bounded by `RedisOptions`:

- any non-cancellation cache read failure is logged and treated as a miss, so persistence resolves
  the redirect;
- a failed fill is logged and the uncached persisted result remains valid;
- a failed invalidation is logged and the persisted redirect-state guard prevents a surviving
  stale value from authorizing a redirect;
- cancellations are not swallowed.

There is no application-layer retry and no fallback to an instance-local cache. This preserves
correctness and avoids divergent per-instance state while Redis reconnects in the background.

## Manual Multi-Instance Verification

Run Redis and SQL Server, apply migrations, then start two API instances with the same SQL Server
connection string, Redis connection string, and Redis key prefix but different HTTP ports. Use
instance A to create and warm a link. Use instance B to update the destination, deactivate,
reactivate, delete, and restore it, checking instance A after every commit. Expected results are
the new destination, `404`, `302`, `404`, and `302`. The shared `redirect:v1:<short-code>` key must
disappear on every mutation and be repopulated only by a later successful redirect.

For outage verification, warm a link, stop Redis, mutate the link, and resolve it while Redis is
unavailable. The request must use persisted state. After Redis returns, a surviving stale value
must fail the persisted state guard, be replaced, and never return the old destination.

TASK-025 verification on 2026-08-12 exercised a miss followed by a shared-cache hit (`302`), a
malformed payload that was evicted and repopulated (`302` with an approximately 24-hour TTL), a
stale inactive row (`404`), deleted and unknown rows (`404`), and an expired row (`410`). A second
API instance configured against unused Redis port 6399 still resolved the valid persisted row with
`302`. Four successful requests produced exactly four click-count increments and four access-log
rows, confirming analytics-write behavior remained synchronous. All temporary database rows,
access logs, Redis keys, and API processes were removed afterward. A final smoke test after the
redirect-only persistence projection repeated miss/hit `302`, the approximately 24-hour TTL, and
expired `410` successfully.
