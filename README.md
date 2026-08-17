# URL Shortener API

## Overview
URL Shortener API is a .NET 10 ASP.NET Core Web API for creating and managing owned short URLs, redirecting with short codes, tracking clicks, viewing stats, and handling deactivate/delete/restore lifecycle operations.

## Local Setup

### 1) Prerequisites
- .NET 10 SDK (the repository `global.json` accepts the latest installed 10.0.1xx patch)
- SQL Server (LocalDB or a full SQL Server instance)
- Redis on `127.0.0.1:6379` (or Docker for the provisional local instance)
- Node.js `^20.19.0`, `^22.12.0`, or `>=24.0.0` and npm `>=10.0.0` for the Angular client

### 2) Configuration
Development defaults to the non-production in-memory repository. To use SQL Server, supply the connection string through user secrets, an environment-specific untracked configuration source, or an environment variable:

```powershell
$env:ConnectionStrings__SqlServer = "Server=(localdb)\MSSQLLocalDB;Database=UrlShortenerDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
```

For a local in-memory run, use the Development environment and keep:

```json
{
  "Storage": {
    "UseInMemory": true
  }
}
```

Set `UseInMemory` to `false` when using SQL Server. In-memory storage is rejected outside Development and loses data on shutdown. See [Persistence and Migrations](docs/persistence.md) for all validated settings and limitations.

Authentication always requires SQL Server. Generate a random 32-byte JWT signing key and supply it through secrets or an environment variable:

```powershell
$jwtKeyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($jwtKeyBytes)
$rng.Dispose()
$env:Identity__JwtSigningKeyBase64 = [Convert]::ToBase64String($jwtKeyBytes)
$env:Storage__UseInMemory = "false"
```

See [Authentication and Session API](docs/authentication.md) for endpoint contracts, refresh-cookie/CSRF handling, expiration, revocation, and all identity configuration.
See [Authorization Boundaries](docs/authorization.md) for protected URL-management routes and owner-scoped access semantics.
See [URL Creation Contract](docs/url-creation.md) for validation, UTC expiry, entropy, and concurrency-safe uniqueness behavior.
See [Idempotency and Request Resilience](docs/idempotency-request-resilience.md) for safe URL-create retries, request limits, timeouts, dependency retry boundaries, and cancellation.
See [Owned URL Query API](docs/owned-url-query.md) for dashboard listing, search, filters, sorting, deletion visibility, and pagination.
See [Owned URL Mutation Lifecycle](docs/url-lifecycle.md) for update/status/delete/restore contracts. The restore window is configured with `ShortUrlLifecycle:SoftDeleteRetentionDays` and defaults to 30 days.
See [Management API Contract](docs/management-api.md) for the finalized Angular-facing resource, collection, error, UTC timestamp, and public URL contracts.

Redis infrastructure is configured separately from redirect cache policy. Development uses the
credential-free loopback endpoint `127.0.0.1:6379`; deployed environments must provide
`Redis__ConnectionString` and an environment-specific `Redis__KeyPrefix` through configuration and
secret sources. See [Redis Infrastructure](docs/redis.md) for local Docker commands, validated
settings, key namespaces, connection lifetime, and outage behavior. See
[Distributed Redirect Cache](docs/redirect-cache.md) for the shared key, payload, absolute TTL,
invalidation, race-safety, corruption recovery, and persistence-fallback contract.
See [Distributed Rate Limiting](docs/rate-limiting.md) for the shared policy matrix, identities,
algorithms, safe configuration bounds, Redis key expiry, `429` retry metadata, and outage behavior.
See [Client IP and Reverse-Proxy Trust](docs/proxy-trust.md) before deploying behind a proxy or
load balancer. Forwarded client IPs are disabled by default and require explicit trusted
proxy/network configuration.

### 3) Database setup
Restore the repository-pinned EF CLI:

```bash
dotnet tool restore
```

Apply migrations:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Storage__UseInMemory = "false"
dotnet ef database update --project UrlShortener.Infrastructure --startup-project UrlShortener.Api
```

### 4) Run the API

```bash
cd UrlShortener.Api
dotnet run
```

### 5) Swagger
After startup, open:
- `https://localhost:7221/swagger`
- `http://localhost:5034/swagger`

### 6) Run the Angular client

Install and verify the pinned frontend toolchain from `web/`:

```powershell
cd web
npm ci
npm run format:check
npm run lint
npm run build
```

For local development, keep the API running on its documented HTTP profile and start Angular:

```powershell
npm start
```

Open `http://localhost:4200`. Development API requests use the centralized
`http://localhost:5034/api/v1` base URL. Production builds use same-origin `/api/v1`; see
[the Angular workspace guide](web/README.md) for the environment and deployment strategy.

## Public Short URL Base

`PublicUrls:BaseUrl` is the canonical externally reachable origin used for every returned `shortUrl`. Development defaults to `https://localhost:7221`; deployed environments must supply their public origin, normally through `PublicUrls__BaseUrl`. The API does not construct public URLs from the request host or an internal reverse-proxy/container host. See [Management API Contract](docs/management-api.md) for the proxy routing model.

## Endpoints

### Authentication `/api/v1/auth`

- `POST /register`: create an account and session.
- `POST /sign-in`: authenticate with email and password.
- `GET /me`: return safe current-user/session metadata; requires a bearer access token.
- `POST /refresh`: rotate the HttpOnly refresh cookie; requires the documented origin and antiforgery inputs.
- `POST /sign-out`: revoke the refresh family and delete the cookie.

Access tokens are returned in JSON and expire after 10 minutes by default. Raw refresh tokens are never returned in response bodies or stored in plaintext.

### 1) GET `/api/v1/short-urls`
Lists only the authenticated user's short URLs. It supports bounded pagination, search, active/expiration/created-date filters, optional deleted-row inclusion, and deterministic sorting. The default page is 1 with 20 items; the maximum page size is 100.

```bash
curl "https://localhost:7221/api/v1/short-urls?page=1&pageSize=20&expiration=notExpired&sortBy=createdAt&sortDirection=desc" \
  -H "Authorization: Bearer $accessToken"
```

See [Owned URL Query API](docs/owned-url-query.md) for the complete request and response contract.

### 2) POST `/api/v1/short-urls`
Creates a short URL. This and every `/api/v1/short-urls` management endpoint require the authenticated owner's bearer access token; missing or invalid tokens return `401 AUTHENTICATION_REQUIRED`.

Request with only `originalUrl`:

```bash
curl -X POST "https://localhost:7221/api/v1/short-urls" \
  -H "Authorization: Bearer $accessToken" \
  -H "Idempotency-Key: 4ac7854d-10b2-47d0-af2f-53a90fa944f0" \
  -H "Content-Type: application/json" \
  -d "{\"originalUrl\":\"https://example.com/articles/hello\"}"
```

Request with `originalUrl + customAlias + expiresAtUtc`:

```bash
curl -X POST "https://localhost:7221/api/v1/short-urls" \
  -H "Authorization: Bearer $accessToken" \
  -H "Content-Type: application/json" \
  -d "{\"originalUrl\":\"https://example.com/docs\",\"customAlias\":\"myAlias_01\",\"expiresAtUtc\":\"2026-12-31T00:00:00Z\"}"
```

Sample success response (`201 Created`):

```json
{
  "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
  "originalUrl": "https://example.com/docs",
  "shortCode": "myAlias_01",
  "shortUrl": "https://localhost:7221/r/myAlias_01",
  "createdAtUtc": "2026-02-18T16:10:00Z",
  "expiresAtUtc": "2026-12-31T00:00:00Z",
  "isActive": true,
  "isExpired": false,
  "isDeleted": false,
  "deletedAtUtc": null,
  "restoreUntilUtc": null,
  "clickCount": 0,
  "lastAccessedAtUtc": null
}
```

Status codes:
- `201 Created`: short URL created
- `400 Bad Request`: validation error (invalid URL, invalid alias, invalid expiry, missing body)
- `409 Conflict`: alias already exists (`ALIAS_CONFLICT`)
- `409 Conflict`: an idempotency key was reused with different content (`IDEMPOTENCY_KEY_REUSED`)
- `413 Payload Too Large`: the create body exceeds 8 KiB (`REQUEST_TOO_LARGE`)
- `429 Too Many Requests`: the per-user creation token bucket is exhausted (`Retry-After` and
  `Cache-Control: no-store` headers are returned)
- `500 Internal Server Error`: generated-code creation exhausted its five collision attempts (`SHORTCODE_GENERATION_FAILED`) or an unexpected persistence failure occurred
- `504 Gateway Timeout`: request execution exceeded the configured bound (`REQUEST_TIMEOUT`)

Generated codes are eight case-sensitive base-62 characters. `originalUrl` is limited to 2,048 characters, and a supplied `expiresAtUtc` must be a future UTC timestamp ending in `Z`.

### 3) GET `/r/{shortCode}`
Redirects to the original URL. This public route does not require authentication.

Test redirect headers/status:

```bash
curl -i "https://localhost:7221/r/myAlias_01"
```

Status codes:
- `302 Found`: redirect to the original URL (`Location` header)
- `404 Not Found`: short code does not exist, deleted, or inactive
- `410 Gone`: short URL exists but is expired

### 4) GET `/api/v1/short-urls/{shortCode}`
Returns short URL details.

```bash
curl "https://localhost:7221/api/v1/short-urls/myAlias_01" \
  -H "Authorization: Bearer $accessToken"
```

Sample response (`200 OK`):

```json
{
  "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
  "originalUrl": "https://example.com/docs",
  "shortCode": "myAlias_01",
  "shortUrl": "https://localhost:7221/r/myAlias_01",
  "createdAtUtc": "2026-02-18T16:10:00Z",
  "expiresAtUtc": "2026-12-31T00:00:00Z",
  "isActive": true,
  "isExpired": false,
  "isDeleted": false,
  "clickCount": 4,
  "lastAccessedAtUtc": "2026-02-18T16:20:30Z"
}
```

Status codes:
- `200 OK`
- `404 Not Found`

### 5) PATCH `/api/v1/short-urls/{shortCode}/status`
Updates active/inactive state.

```bash
curl -X PATCH "https://localhost:7221/api/v1/short-urls/myAlias_01/status" \
  -H "Authorization: Bearer $accessToken" \
  -H "Content-Type: application/json" \
  -d "{\"isActive\":false}"
```

Sample response (`200 OK`):

```json
{
  "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
  "originalUrl": "https://example.com/docs",
  "shortCode": "myAlias_01",
  "shortUrl": "https://localhost:7221/r/myAlias_01",
  "createdAtUtc": "2026-02-18T16:10:00Z",
  "expiresAtUtc": "2026-12-31T00:00:00Z",
  "isActive": false,
  "isExpired": false,
  "isDeleted": false,
  "clickCount": 4,
  "lastAccessedAtUtc": "2026-02-18T16:20:30Z"
}
```

Status codes:
- `200 OK`
- `400 Bad Request`: missing body or omitted/non-Boolean `isActive`
- `404 Not Found`

### 6) PUT `/api/v1/short-urls/{shortCode}`
Replaces the mutable destination and expiry fields. Omitting or setting `expiresAtUtc` to `null` clears expiry. The alias and ownership are immutable.

```bash
curl -X PUT "https://localhost:7221/api/v1/short-urls/myAlias_01" \
  -H "Authorization: Bearer $accessToken" \
  -H "Content-Type: application/json" \
  -d "{\"originalUrl\":\"https://example.com/new-destination\",\"expiresAtUtc\":null}"
```

Status codes:
- `200 OK`: updated details
- `400 Bad Request`: invalid destination/expiry or missing body
- `404 Not Found`

### 7) DELETE `/api/v1/short-urls/{shortCode}`
Soft deletes a short URL.

```bash
curl -X DELETE "https://localhost:7221/api/v1/short-urls/myAlias_01" \
  -H "Authorization: Bearer $accessToken"
```

Status codes:
- `204 No Content`: deleted
- `404 Not Found`

### 8) POST `/api/v1/short-urls/{shortCode}/restore`
Restores a soft-deleted owned URL before its retention boundary (30 days by default).

```bash
curl -X POST "https://localhost:7221/api/v1/short-urls/myAlias_01/restore" \
  -H "Authorization: Bearer $accessToken"
```

Status codes:
- `200 OK`: restored details
- `404 Not Found`
- `409 Conflict`: link is not deleted (`RESTORE_NOT_DELETED`)
- `410 Gone`: restore window expired (`RESTORE_WINDOW_EXPIRED`)

See [Owned URL Mutation Lifecycle](docs/url-lifecycle.md) for alias immutability, retention, cache invalidation, and concurrency semantics.

### 9) GET `/api/v1/short-urls/{shortCode}/stats?fromUtc=&toUtc=`
Returns click stats.

Without query params (defaults to last 30 days):

```bash
curl "https://localhost:7221/api/v1/short-urls/myAlias_01/stats" \
  -H "Authorization: Bearer $accessToken"
```

With `fromUtc`/`toUtc`:

```bash
curl "https://localhost:7221/api/v1/short-urls/myAlias_01/stats?fromUtc=2026-02-01T00:00:00Z&toUtc=2026-02-18T23:59:59Z" \
  -H "Authorization: Bearer $accessToken"
```

Sample response (`200 OK`):

```json
{
  "shortCode": "myAlias_01",
  "totalClicks": 7,
  "fromUtc": "2026-02-01T00:00:00Z",
  "toUtc": "2026-02-18T23:59:59Z",
  "dailyClicks": [
    {
      "dateUtc": "2026-02-14",
      "clicks": 2
    },
    {
      "dateUtc": "2026-02-18",
      "clicks": 5
    }
  ]
}
```

Status codes:
- `200 OK`
- `404 Not Found`

## Error Response Format
All API errors use this shape:

```json
{
  "traceId": "00-7fce46c2f2f2f8f5d28ec9bd6a30c8f4-7d9fd6a4b482a8a5-00",
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed.",
    "details": [
      {
        "field": "originalUrl",
        "message": "OriginalUrl must be an absolute http or https URL."
      }
    ]
  }
}
```

Validation error example (`400`):

```json
{
  "traceId": "00-4a764f3b1d5d67dcf4f4f45f6dbbf58d-2227c542ed53d4fc-00",
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed.",
    "details": [
      {
        "field": "originalUrl",
        "message": "OriginalUrl must be an absolute http or https URL."
      }
    ]
  }
}
```

Rate limited example (`429`):

```json
{
  "traceId": "00-55f39de9efea9b784be68ac9fd96f8b7-b63f7f5f3bbf8bb2-00",
  "error": {
    "code": "RATE_LIMITED",
    "message": "Rate limit exceeded. Retry after 27 seconds.",
    "details": []
  }
}
```

Also returned for `429`: `Retry-After` response header.
