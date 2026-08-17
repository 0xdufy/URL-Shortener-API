# Baseline API, Data, and Runtime Contracts

**Frozen from:** Phase 00 working tree on 2026-08-11  
**Purpose:** Regression reference, not an endorsement of every behavior

## HTTP surface

The API has no authentication or ownership boundary. Swagger/OpenAPI is enabled in all environments. JSON request/response models returned through MVC use camelCase. The redirect endpoint is deliberately outside the versioned API route.

### POST `/api/v1/short-urls`

Creates a short URL. The process-local creation rate limit is evaluated before body/model validation, so rejected requests consume the current IP bucket.

Request:

```json
{
  "originalUrl": "https://example.com/path",
  "customAlias": "Docs_01",
  "expiresAtUtc": "2026-12-31T00:00:00Z"
}
```

`customAlias` and `expiresAtUtc` may be omitted or `null`. Whitespace-only aliases are treated like no alias. Unknown JSON properties are ignored by the default serializer.

`201 Created` response:

```json
{
  "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
  "originalUrl": "https://example.com/path",
  "shortCode": "Docs_01",
  "shortUrl": "http://localhost:5034/r/Docs_01",
  "createdAtUtc": "2026-08-11T16:00:00Z",
  "expiresAtUtc": "2026-12-31T00:00:00Z",
  "isActive": true,
  "clickCount": 0
}
```

The `Location` header is the relative management route `/api/v1/short-urls/{shortCode}`. `shortUrl` uses the incoming request scheme and Host header.

Statuses:

- `201`: created.
- `400`: missing/null/malformed body, invalid URL/alias/expiry, or JSON/model-binding validation failure.
- `409`: a custom alias was found by the application pre-check.
- `429`: fixed-minute create limit exceeded; includes integer-seconds `Retry-After`.
- `500`: unexpected failure or five failed generated-code pre-check attempts. A database uniqueness race also currently falls through as an unexpected `500`.

### GET `/api/v1/short-urls/{shortCode}`

Returns a non-deleted record regardless of active or expired state.

`200 OK` response:

```json
{
  "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
  "originalUrl": "https://example.com/path",
  "shortCode": "Docs_01",
  "createdAtUtc": "2026-08-11T16:00:00Z",
  "expiresAtUtc": null,
  "isActive": true,
  "isDeleted": false,
  "clickCount": 3,
  "lastAccessedAtUtc": "2026-08-11T16:10:00Z"
}
```

Returns `404` with `NOT_FOUND` for missing or soft-deleted codes.

### PATCH `/api/v1/short-urls/{shortCode}/status`

Request:

```json
{
  "isActive": false
}
```

Returns `200` with the same details shape as GET and removes that code from the process-local cache. Returns `400` for an invalid/missing body and `404` for a missing or deleted code. JSON omission of `isActive` binds to `false`; the current validator accepts either Boolean value and cannot distinguish omission.

### DELETE `/api/v1/short-urls/{shortCode}`

Soft-deletes a non-deleted record, sets `deletedAtUtc` to the application UTC clock, and invalidates its local cache entry. Returns `204 No Content`. Missing and already-deleted codes return `404`.

There is no restore endpoint. The database row, short-code uniqueness claim, and access logs remain after soft deletion.

### GET `/api/v1/short-urls/{shortCode}/stats`

Optional query keys are `fromUtc` and `toUtc`, parsed as nullable `DateTime` values. Defaults are application UTC now minus 30 days and now. Both bounds are inclusive. There is no explicit UTC-kind, maximum-range, or `fromUtc <= toUtc` validation.

`200 OK` response:

```json
{
  "shortCode": "Docs_01",
  "totalClicks": 2,
  "fromUtc": "2026-07-12T16:00:00Z",
  "toUtc": "2026-08-11T16:00:00Z",
  "dailyClicks": [
    {
      "dateUtc": "2026-08-11",
      "clicks": 2
    }
  ]
}
```

`totalClicks` is the sum inside the requested range, not the entity's lifetime counter. Days with zero clicks are omitted. Missing/deleted codes return `404`; inactive and expired codes remain queryable. Because automatic model-state responses are suppressed and this action does not inspect model state, invalid date query text may fall back to null/default range instead of returning a normalized `400`.

### GET `/r/{shortCode}`

| Stored state | Result |
|---|---|
| Active, non-deleted, not expired | Synchronously records baseline analytics, best-effort publishes a click event, then returns `302 Found` to `OriginalUrl` |
| Missing | `404 NOT_FOUND` |
| Soft-deleted | `404 NOT_FOUND` |
| Inactive | `404 NOT_FOUND` |
| `ExpiresAtUtc <= now` | `410 EXPIRED` |

The endpoint reads cache first and then persistence. It rechecks active/deleted/expiry during the click-count update. A successful redirect currently increments the baseline count, updates `LastAccessedAtUtc`, inserts an access log with IP/user agent/referrer, and saves it. After that authoritative guard succeeds, it makes a bounded best-effort publication of the privacy-aware `analytics.click` version 1 event. Broker failure is logged and does not deny the redirect. If the conditional update says the record is no longer redirectable, the cache entry is removed, no successful-click event is emitted, and the request ultimately resolves from current persistence state. TASK-032 removes the transitional synchronous analytics write after TASK-031 supplies idempotent worker persistence.

ASP.NET's `Redirect(string)` produces a temporary `302`; redirects are not permanently cacheable by contract.

## Validation and short-code rules

- `originalUrl` is required and must parse as an absolute `http` or `https` URI. There is no application-level maximum, although SQL Server limits it to 2,048 characters.
- A nonblank custom alias must be 4–20 characters and match `^[A-Za-z0-9_-]+$`.
- Generated codes are six characters from ASCII upper/lowercase letters and digits, using cryptographic random bytes. Generation tries at most five codes after application-level existence checks.
- Codes and aliases are case-sensitive in the in-memory dictionary and SQL Server's `Latin1_General_CS_AS` column/query collation.
- SQL Server has a unique index on `ShortCode`; soft deletion does not release a code. The in-memory repository also retains the code mapping.
- `expiresAtUtc`, when supplied, must be later than `DateTime.UtcNow` at validation time. The validator directly reads the system clock.
- Creation is check-then-insert. Concurrent requests can both pass the existence check. SQL Server rejects the later insert through its unique index, but the exception is not translated to `409`; in-memory add is not equivalent to relational uniqueness and can overwrite the code-to-ID mapping.

## Error envelopes and status mapping

Controller-created errors pass through MVC and use camelCase:

```json
{
  "traceId": "0HN...:00000001",
  "error": {
    "code": "NOT_FOUND",
    "message": "Short URL not found.",
    "details": []
  }
}
```

`details` entries have `field` and `message`. Controller paths currently produce:

- `NOT_FOUND` / `Short URL not found.` for management misses and redirect missing/inactive/deleted.
- `EXPIRED` / `Short URL has expired.` for redirect expiry.
- `RATE_LIMITED` / `Too many requests. Retry after {n} seconds.` for create throttling.

Exceptions handled by `ExceptionHandlingMiddleware` are serialized with a new default `JsonSerializer` configuration, not MVC's web JSON options. Their current JSON field names are therefore PascalCase (`TraceId`, `Error`, `Code`, `Message`, `Details`, `Field`, `Message`). Mappings are:

| Exception | Status | Code | Message |
|---|---:|---|---|
| FluentValidation `ValidationException` | 400 | `VALIDATION_ERROR` | `Validation failed.` |
| `AliasConflictException` | 409 | `ALIAS_CONFLICT` | `Alias already exists.` |
| `NotFoundException` | 404 | `NOT_FOUND` | `Resource not found.` |
| `ExpiredException` | 410 | `EXPIRED` | `Short URL has expired.` |
| `RateLimitedException` | 429 | `RATE_LIMITED` | Exception message |
| `ShortCodeGenerationFailedException` | 500 | `SHORTCODE_GENERATION_FAILED` | `Failed to generate short code.` |
| Any other exception | 500 | `UNEXPECTED_ERROR` | `Unexpected error.` |

Only validation, alias conflict, generation failure, and unexpected exceptions are reachable through current service/controller flows; the other exception mappings are presently unused. Framework-generated 404/405/415 responses and failures outside the custom middleware are not guaranteed to use either envelope.

## Persistence contract

### `ShortUrls`

| Field | SQL shape / rule |
|---|---|
| `Id` | `uniqueidentifier`, primary key |
| `OriginalUrl` | `nvarchar(2048)`, required |
| `ShortCode` | `nvarchar(20)`, required, case-sensitive collation, unique index |
| `CreatedAtUtc` | `datetime2`, required |
| `ExpiresAtUtc` | nullable `datetime2`, indexed |
| `IsActive` | `bit`, required, default true |
| `IsDeleted` | `bit`, required, default false, indexed |
| `DeletedAtUtc` | nullable `datetime2` |
| `ClickCount` | `bigint`, required, default 0 |
| `LastAccessedAtUtc` | nullable `datetime2` |

### `ShortUrlAccessLogs`

`Id` is the primary key; `ShortUrlId` is a required cascade-delete foreign key; `AccessedAtUtc` is required `datetime2`; optional lengths are IP 64, user agent 256, and referrer 512. `(ShortUrlId, AccessedAtUtc)` is indexed for range/group queries.

SQL Server is the production mode. The Development-only in-memory mode is non-durable, process-local, and does not enforce EF lengths, relational transactions, or fully equivalent uniqueness behavior.

## Runtime configuration

| Key | Baseline default / requirement |
|---|---|
| `Storage:UseInMemory` | `false` in base settings; `true` in Development; startup rejects `true` outside Development |
| `ConnectionStrings:SqlServer` | Required and nonblank when in-memory is false; Development contains a LocalDB trusted-connection example |
| `Persistence:MaxRetryCount` | Default 3; valid 0–10 |
| `Persistence:MaxRetryDelaySeconds` | Default 5; valid 1–60 |
| `Persistence:CommandTimeoutSeconds` | Default 30; valid 1–300 |
| `RateLimiting:CreatePerMinuteLimit` | Default 20; valid 1–10,000 |
| `Serilog:MinimumLevel` | Default/configured `Information`; unparseable values fall back to Information |

SQL configuration enables provider-recognized transient retries and the configured command timeout. The API never migrates the database automatically. Logs go to console and rolling `logs/url-shortener-.log` files.

## Cache and rate-limit contract

- Cache implementation: singleton wrapper over `IMemoryCache`; key `su:{shortCode}`.
- Fill: after creation and after an uncached successful persistence lookup on redirect.
- Invalidation: status change, soft deletion, or failed cached conditional access update.
- TTL: time until link expiry, clamped to at least one minute; otherwise 24 hours. Resolver still checks cached expiry on every request. There is no negative caching.
- Scope: one process. Other instances neither share entries nor receive invalidations.
- Create limiter: singleton fixed UTC-minute counter keyed `rl:create:{remoteIp}:{yyyyMMddHHmm}`.
- Limit: only POST creation, per directly observed remote IP; expiry at next UTC minute; rejected requests return remaining time rounded up through `Retry-After`.
- Scope: one process. Limits multiply across instances, reset on restart, and do not honor forwarded IP headers.

## Candidates for deliberate change

| Current assumption/behavior | Candidate roadmap owner |
|---|---|
| Mixed camelCase/PascalCase error serialization and non-enveloped framework errors | Phase 03 API contract hardening, with Phase 12 HTTP hardening |
| No authentication, ownership, or authorization | Phases 02–03 |
| Check-then-insert creation and unhandled database uniqueness races | Phase 03 |
| Missing query-range/UTC validation and ambiguous stats `totalClicks` | Phases 03 and 09 |
| Incoming Host determines returned public URL | Phase 12 configuration/HTTP hardening |
| Process-local redirect cache and invalidation | Phase 06 |
| Process-local IP creation rate limiter and no proxy trust policy | Phase 07 (superseded; see `rate-limiting.md` and `proxy-trust.md`) |
| Synchronous analytics writes on redirect | Phase 08 |
| Raw IP/user-agent/referrer retention without privacy/retention policy | Phases 09, 12, and 13 |
| Swagger enabled in every environment | Phase 12 |
| No health, metrics, or tracing endpoints | Phase 14 |

Changes to these behaviors must be intentional, documented, and verified by their owning phase; Phase 00 does not silently correct them.
