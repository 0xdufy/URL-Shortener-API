# Idempotency and Request Resilience

## URL creation retry contract

`POST /api/v1/short-urls` accepts an optional `Idempotency-Key` request header. Clients that may
retry a create request after a timeout or lost response should generate a new key for each logical
creation and reuse that key only for retries of that same payload.

Keys must be a single 16-128 character value containing only ASCII letters, digits, `.`, `_`, `:`,
or `-`. UUIDs are valid. Missing keys retain the ordinary one-attempt create behavior; malformed
keys return `400 VALIDATION_ERROR` with an `idempotencyKey` detail.

For a valid key:

- The key is scoped to the authenticated user ID. Identical key text used by another user is an
  independent operation and cannot retrieve the first user's result.
- The raw key is never persisted. SQL Server stores its SHA-256 hash, a versioned canonical request
  hash, the resulting short-link ID, and creation/expiry timestamps.
- The short link and idempotency record are saved atomically. A unique `(OwnerId, KeyHash)` index is
  the concurrency authority, so concurrent retries resolve to one link even with a custom alias.
- A retry with the same material payload returns `201 Created`, the same logical link ID and short
  code, and the same management `Location`. The resource representation reflects its current
  persisted state if it was changed after creation.
- Reusing the key with a different `originalUrl`, effective `customAlias`, or `expiresAtUtc` returns
  `409 IDEMPOTENCY_KEY_REUSED`. Blank aliases have the same create meaning as an omitted alias.

Records expire after `Idempotency:RetentionHours`, which defaults to 24 and is startup-validated
from 1 through 168 hours. Every keyed create removes expired records through the indexed expiry
column before resolving its key. Consequently, expired state cannot grow while creation traffic
continues and cannot grow at all while the API is idle. Phase 13 retention maintenance may perform
the same cleanup independently. After expiry, the key is new again; the request may produce a new
generated link or encounter the ordinary alias conflict for an alias that still exists.

## Request and dependency bounds

The defaults in `RequestLimits` are startup-validated:

| Boundary | Default | Valid configuration range |
|---|---:|---:|
| Server request body | 16,384 bytes | 8,192-1,048,576 bytes |
| Create request body | 8,192 bytes | Fixed endpoint limit |
| Request line | 8,192 bytes | 2,048-32,768 bytes |
| All request headers | 16,384 bytes | 8,192-65,536 bytes |
| Header count | 64 | 16-128 |
| Header receive timeout | 10 seconds | 5-60 seconds |
| Request execution timeout | 15 seconds | 5-120 seconds |

The 8 KiB create limit allows the documented 2,048-character destination plus JSON and optional
fields with margin. An oversized body handled in the application pipeline returns the common
`413 REQUEST_TOO_LARGE` envelope. Kestrel or a reverse proxy may reject an oversized request line
or header block before the application pipeline; the edge proxy must enforce limits no larger than
its own capacity and should normalize those edge-generated errors where a uniform external body is
required.

SQL commands retain the validated `Persistence:CommandTimeoutSeconds` bound (30 seconds by
default). Automatic EF SQL retries are disabled: replaying an ambiguous non-idempotent write can
duplicate effects, so clients retry URL creation only through `Idempotency-Key`. Redis connection
establishment has at most `Redis:ConnectRetryCount` retries and bounded connect/reconnect delays.
Application Redis commands are not replayed blindly; they use the configured operation timeout,
and the rate limiter fails closed while redirect caching follows its documented fallback behavior.
The API currently has no outbound HTTP dependency.

The request execution timeout and client disconnect both cancel `HttpContext.RequestAborted`.
Controller validation, application services, EF queries/saves, distributed cache calls, and Redis
rate-limit waits receive that token where their APIs support cancellation. A server-side Redis
command may finish after the caller stops waiting, but its client wait remains bounded by both
cancellation and the Redis operation timeout. Execution timeouts return `504 REQUEST_TIMEOUT` with
`Cache-Control: no-store`; client disconnects do not generate a misleading internal-error response.

## Manual verification

Use SQL Server rather than the development-only in-memory repository to exercise multi-request
transaction and unique-index behavior.

1. Register two users and obtain their bearer tokens.
2. As User A, create a link with a fresh valid key, then repeat the exact request. Confirm both
   responses are `201` and contain the same `id` and `shortCode`.
3. Send two concurrent creates with the same user, key, payload, and custom alias. Confirm both
   return `201` with one logical ID and only one short-link row exists.
4. Reuse User A's key with a changed destination, alias, or expiry. Confirm
   `409 IDEMPOTENCY_KEY_REUSED` in the common error envelope.
5. As User B, reuse the same key text. Confirm a distinct link is created and User A's result is not
   returned.
6. Inspect the idempotency row and confirm its expiry is the configured retention interval. In a
   disposable database, move that expiry into the past and repeat the generated-link request;
   confirm cleanup occurs and a new logical result is created.
7. Send more than 8 KiB to the create endpoint and confirm `413 REQUEST_TOO_LARGE`. Send a short or
   otherwise invalid key and confirm `400 VALIDATION_ERROR`.
8. In a disposable environment, make SQL connection establishment or a command exceed the request
   timeout. Confirm `504 REQUEST_TIMEOUT`, observe cancellation in the EF operation, and verify the
   response contains no connection string, SQL message, Redis endpoint, or stack trace.
9. Start a valid request and abort the client connection. Confirm downstream work receives
   cancellation and the server does not log it as an unexpected application failure.

Automated coverage for these cases remains deferred to Phase 16.
