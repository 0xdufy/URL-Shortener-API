# Owned URL Query API

`GET /api/v1/short-urls` returns a paginated collection containing only short URLs owned by the authenticated user. A valid bearer access token is required. Legacy unowned rows and rows belonging to another user cannot enter the query result.

## Query contract

| Parameter | Default | Rules |
|---|---:|---|
| `page` | `1` | One-based integer greater than or equal to 1. |
| `pageSize` | `20` | Integer from 1 through 100. |
| `search` | none | Maximum 200 characters; contains search across short code and destination URL. Short-code matching remains case-sensitive. |
| `isActive` | none | `true` or `false`; omitted means both states. |
| `expiration` | `all` | `all`, `expired`, or `notExpired`. A link expiring exactly at request time is expired; a link without an expiry is not expired. |
| `includeDeleted` | `false` | Soft-deleted links are excluded by default and included when `true`. Included rows expose `isDeleted` so clients can distinguish them. |
| `createdFromUtc` | none | Inclusive lower creation-time bound using `Z` or `+00:00`. |
| `createdToUtc` | none | Inclusive upper creation-time bound using `Z` or `+00:00`, greater than or equal to `createdFromUtc`. |
| `sortBy` | `createdAt` | `createdAt`, `shortCode`, `clickCount`, or `expiresAt`. |
| `sortDirection` | `desc` | `asc` or `desc`. |

Every ordering appends `id` in the same direction as a stable tie-breaker. This makes the ordering deterministic across pages for an unchanged data set. This is offset pagination, so concurrent inserts or mutations can still shift later pages.

Invalid model-bound values and unsupported filter or sort values return `400 VALIDATION_ERROR` using the common error envelope.

Example:

```bash
curl "https://localhost:7221/api/v1/short-urls?page=1&pageSize=20&search=docs&isActive=true&expiration=notExpired&sortBy=createdAt&sortDirection=desc" \
  -H "Authorization: Bearer $accessToken"
```

Example response:

```json
{
  "items": [
    {
      "id": "c8e39a3e-7dd1-47f1-9c2f-e2e3130fef85",
      "originalUrl": "https://example.com/docs",
      "shortCode": "myAlias_01",
      "shortUrl": "https://sho.rt/r/myAlias_01",
      "createdAtUtc": "2026-08-11T18:00:00Z",
      "expiresAtUtc": "2026-12-31T00:00:00Z",
      "isActive": true,
      "isExpired": false,
      "isDeleted": false,
      "clickCount": 4
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
    "search": "docs",
    "isActive": true,
    "expiration": "notExpired",
    "includeDeleted": false,
    "createdFromUtc": null,
    "createdToUtc": null,
    "sortBy": "createdAt",
    "sortDirection": "desc"
  }
}
```

The `filters` object reports the normalized values the server applied. Each item includes the configured canonical `shortUrl`, so clients do not reconstruct redirect URLs.

## Query shape and indexes

The SQL repository starts with `OwnerId`, applies deletion/search/state/expiration/date predicates, counts the filtered query, orders it, and then projects only list fields before `Skip`/`Take`. It never materializes the owner's full entity collection.

The index `IX_ShortUrls_OwnerId_IsDeleted_CreatedAtUtc_Id` supports the common default path: owner isolation, exclusion of deleted rows, creation-time ordering, and the stable ID tie-breaker. Additional indexes for every optional sort/filter combination are intentionally omitted to avoid write amplification for low-selectivity Boolean filters. Contains searches over arbitrary URL substrings cannot use a conventional leading-key B-tree index; SQL Server full-text search can be considered if production measurements show this bounded dashboard search needs it.
