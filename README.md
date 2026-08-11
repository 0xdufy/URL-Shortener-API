# URL Shortener API

## Overview
URL Shortener API is a .NET 10 ASP.NET Core Web API for creating short URLs, redirecting with short codes, tracking clicks, viewing stats, toggling active status, and soft deleting links.

## Local Setup

### 1) Prerequisites
- .NET 10 SDK (the repository `global.json` accepts the latest installed 10.0.1xx patch)
- SQL Server (LocalDB or a full SQL Server instance)

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
See [Owned URL Query API](docs/owned-url-query.md) for dashboard listing, search, filters, sorting, deletion visibility, and pagination.

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

## Base URL Note
The returned `shortUrl` is built from the incoming request scheme and host (`Request.Scheme` + `Request.Host`).

Example: if you call `POST` on `https://localhost:7221`, the API returns `shortUrl` like `https://localhost:7221/r/{shortCode}`.

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
  "clickCount": 0
}
```

Status codes:
- `201 Created`: short URL created
- `400 Bad Request`: validation error (invalid URL, invalid alias, invalid expiry, missing body)
- `409 Conflict`: alias already exists (`ALIAS_CONFLICT`)
- `429 Too Many Requests`: create rate limit exceeded (`Retry-After` header is returned)
- `500 Internal Server Error`: generated-code creation exhausted its five collision attempts (`SHORTCODE_GENERATION_FAILED`) or an unexpected persistence failure occurred

Generated codes are eight case-sensitive base-62 characters. `originalUrl` is limited to 2,048 characters, and a supplied `expiresAtUtc` must be a future UTC timestamp ending in `Z`.

### 3) GET `/r/{shortCode}`
Redirects to the original URL.

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
  "createdAtUtc": "2026-02-18T16:10:00Z",
  "expiresAtUtc": "2026-12-31T00:00:00Z",
  "isActive": true,
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
  "createdAtUtc": "2026-02-18T16:10:00Z",
  "expiresAtUtc": "2026-12-31T00:00:00Z",
  "isActive": false,
  "isDeleted": false,
  "clickCount": 4,
  "lastAccessedAtUtc": "2026-02-18T16:20:30Z"
}
```

Status codes:
- `200 OK`
- `404 Not Found`

### 6) DELETE `/api/v1/short-urls/{shortCode}`
Soft deletes a short URL.

```bash
curl -X DELETE "https://localhost:7221/api/v1/short-urls/myAlias_01" \
  -H "Authorization: Bearer $accessToken"
```

Status codes:
- `204 No Content`: deleted
- `404 Not Found`

### 7) GET `/api/v1/short-urls/{shortCode}/stats?fromUtc=&toUtc=`
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
    "message": "Too many requests. Retry after 27 seconds.",
    "details": []
  }
}
```

Also returned for `429`: `Retry-After` response header.
