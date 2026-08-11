# Authorization Boundaries

All routes under `/api/v1/short-urls` are protected management endpoints. They require a valid JWT bearer access token, including create, details, status changes, deletion, and analytics. `GET /r/{shortCode}` is a separate public route and remains available without authentication.

## Ownership enforcement

`ShortUrlService` requires a stable current-user ID for every management use case. Reads and mutations use `GetOwnedByShortCodeNotDeletedAsync`, whose persistence predicate combines the case-sensitive short code, authenticated `OwnerId`, and non-deleted state. A row is therefore not loaded into a management use case before ownership has been established.

This boundary applies equally to SQL Server and the Development-only in-memory repository. Legacy rows with `OwnerId IS NULL` are excluded. The public redirect resolver deliberately uses its separate ownership-independent lookup.

Future link edit and restore operations must use the same owner-scoped repository boundary (or a stricter successor). They must never load a short URL globally and authorize it afterward.

## Response policy

| Situation | Status and error code | Policy |
|---|---|---|
| Missing, malformed, expired, or invalid bearer token on a protected endpoint | `401 AUTHENTICATION_REQUIRED` | The caller has no accepted authenticated identity. |
| Authenticated caller requests a missing, deleted, legacy/unowned, or differently owned short code | `404 NOT_FOUND` | Resource existence and ownership are concealed; these states use the same response body. |
| Authenticated caller fails a non-resource policy such as a future role or scope requirement | `403 FORBIDDEN` | The identity is known and resource-existence concealment is not involved. |

Authentication and authorization failures use the common error envelope with a request trace ID and empty details. They do not include token contents, claims, owner IDs, passwords, refresh credentials, or resource-existence hints.

## OpenAPI and manual verification

The controller-level authorization metadata marks every short-URL management operation with the OpenAPI Bearer security requirement. The public redirect operation has no Bearer requirement.

Manual verification uses two independently registered users and one owned link per user:

1. Call each management route without a bearer token and confirm `401 AUTHENTICATION_REQUIRED`.
2. Use User A's token against User B's details, status, deletion, and stats routes and confirm the same `404 NOT_FOUND` response as an unknown code.
3. Confirm User B can still read details, query stats, change status, and delete User B's own link.
4. Call an active link through `/r/{shortCode}` without a bearer token and confirm the public redirect still succeeds.
5. Inspect Swagger/OpenAPI and confirm Bearer security appears on management operations only.
