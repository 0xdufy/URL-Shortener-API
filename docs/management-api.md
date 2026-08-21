# Management API Contract

This is the Angular-facing contract for owned short-link management. All routes are versioned under `/api/v1/short-urls` and require `Authorization: Bearer <access-token>`.

## Public URL origin

Set `PublicUrls:BaseUrl` to the externally reachable HTTPS origin, for example:

```json
{
  "PublicUrls": {
    "BaseUrl": "https://sho.rt",
    "CustomDomainScheme": "https"
  }
}
```

The value is required at startup and must be an absolute HTTP or HTTPS origin without a path, query, fragment, or trailing slash. Environment variable form: `PublicUrls__BaseUrl=https://sho.rt`.

Every link representation includes a canonical `shortUrl` such as `https://sho.rt/r/A1b2C3d4`.
A branded link uses the selected verified persisted hostname and configured custom-domain scheme.
Angular must render or copy that value directly instead of reconstructing it. The API deliberately
does not derive it from `Host` or forwarding headers. See `custom-domain-routing.md` for edge,
DNS, and TLS requirements.

## Link resource

Create, detail, update, status, and restore return the same resource shape:

```json
{
  "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
  "originalUrl": "https://example.com/docs",
  "shortCode": "A1b2C3d4",
  "shortUrl": "https://sho.rt/r/A1b2C3d4",
  "customDomainId": null,
  "customDomainHost": null,
  "createdAtUtc": "2026-08-11T20:10:00Z",
  "expiresAtUtc": null,
  "isActive": true,
  "isExpired": false,
  "isDeleted": false,
  "deletedAtUtc": null,
  "restoreUntilUtc": null,
  "clickCount": 0,
  "lastAccessedAtUtc": null
}
```

All timestamp properties ending in `Utc` are ISO-8601 UTC values serialized with `Z`; nullable timestamps use JSON `null`. `isExpired`, restore eligibility, short URL construction, ownership, and counters are server-owned values.

`clickCount` and `lastAccessedAtUtc` are eventually consistent projections maintained by the
analytics worker. They normally update within a few seconds of a redirect when RabbitMQ, the
worker, and SQL Server are healthy, and may lag longer during retry or outage recovery.

## Collection contract

`GET /api/v1/short-urls` returns list projections plus explicit pagination and normalized applied filters:

```json
{
  "items": [
    {
      "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
      "originalUrl": "https://example.com/docs",
      "shortCode": "A1b2C3d4",
      "shortUrl": "https://sho.rt/r/A1b2C3d4",
      "customDomainId": null,
      "customDomainHost": null,
      "createdAtUtc": "2026-08-11T20:10:00Z",
      "expiresAtUtc": null,
      "isActive": true,
      "isExpired": false,
      "isDeleted": false,
      "deletedAtUtc": null,
      "restoreUntilUtc": null,
      "clickCount": 0
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "filters": {
    "search": null,
    "isActive": null,
    "expiration": "all",
    "includeDeleted": false,
    "createdFromUtc": null,
    "createdToUtc": null,
    "sortBy": "createdAt",
    "sortDirection": "desc"
  }
}
```

Supported query values are documented in Swagger. The defaults are page `1`, page size `20`, maximum page size `100`, non-deleted links only, and newest-created first. `filters` echoes the server-normalized values actually applied.

## Operations

| Operation | Request | Success | Expected errors |
|---|---|---|---|
| `GET /api/v1/short-urls` | Query parameters | `200` collection | `400`, `401`, `403` |
| `POST /api/v1/short-urls` | Optional `Idempotency-Key`; `{ originalUrl, customAlias?, customDomainId?, expiresAtUtc? }` | `201` resource | `400`, `401`, `403`, `409`, `413`, `429`, `500`, `504` |
| `GET /api/v1/short-urls/{shortCode}` | None | `200` resource | `401`, `403`, `404` |
| `GET /api/v1/short-urls/{shortCode}/qr-code` | Bounded SVG options | `200 image/svg+xml` | `400`, `401`, `403`, `404` |
| `PUT /api/v1/short-urls/{shortCode}` | `{ originalUrl, customDomainId?, expiresAtUtc }` | `200` resource | `400`, `401`, `403`, `404`, `409` |
| `PATCH /api/v1/short-urls/{shortCode}/status` | `{ isActive }` | `200` resource | `400`, `401`, `403`, `404` |
| `DELETE /api/v1/short-urls/{shortCode}` | None | `204` | `401`, `403`, `404` |
| `POST /api/v1/short-urls/{shortCode}/restore` | None | `200` resource | `401`, `403`, `404`, `409`, `410` |

The create response includes a relative management `Location` header: `/api/v1/short-urls/{shortCode}`.
Aliases are immutable after creation. `PUT` replaces destination, domain assignment, and expiry;
a null/omitted domain chooses the platform host and a null/omitted expiry clears expiry.

Retry-capable clients should supply a fresh 16-128 character `Idempotency-Key` for each logical
create. Repeating the same accepted payload and key returns the same logical resource with `201`;
changing material payload fields returns `409 IDEMPOTENCY_KEY_REUSED`. Keys are authenticated-user
scoped and retained for 24 hours by default. See
[Idempotency and Request Resilience](idempotency-request-resilience.md).

Every management operation also has a distributed authenticated-user policy and may return
`429 RATE_LIMITED` or `503 RATE_LIMITING_UNAVAILABLE`. Creation overrides that general policy with
its own token bucket, so a create request consumes only the creation bucket. See
[Distributed Rate Limiting](rate-limiting.md) for limits and retry metadata.

## Errors and authorization

Every API error, including malformed JSON, unsupported media types, unknown API routes, authentication challenges, and application errors, uses:

```json
{
  "traceId": "request-correlation-id",
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

An absent, invalid, or expired token returns `401 AUTHENTICATION_REQUIRED`. An authenticated request for a missing, deleted, unowned, or differently owned code returns the same `404 NOT_FOUND` response to conceal resource existence. `403 FORBIDDEN` is reserved for a non-resource role or scope policy. Angular should branch on `error.code`, retain `traceId` for support, and bind validation details by the camel-case `field` value.

Swagger at `/swagger` is the authoritative schema listing and marks every management operation with Bearer authentication.
