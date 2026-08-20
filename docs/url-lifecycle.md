# Owned URL Mutation Lifecycle

All lifecycle endpoints require a bearer token and resolve the current owner through `ICurrentUserContext`. A code that is missing, legacy/unowned, owned by another user, or unavailable because it is deleted returns the same `404 NOT_FOUND` response where applicable; mutation endpoints do not reveal another owner's resource.

## Mutable and immutable fields

`PUT /api/v1/short-urls/{shortCode}` is a full replacement of the three mutable fields:

```json
{
  "originalUrl": "https://example.com/new-destination",
  "customDomainId": null,
  "expiresAtUtc": "2026-12-31T00:00:00Z"
}
```

`originalUrl` is required, limited to 2,048 characters, and must be an absolute HTTP or HTTPS URL.
`expiresAtUtc` may be omitted or `null` to clear expiry; otherwise it must be a future UTC timestamp
ending in `Z`. `customDomainId` is `null`/omitted for the platform host or the ID of a currently
verified, enabled, owner-accessible domain; an unavailable selection returns
`409 CUSTOM_DOMAIN_UNAVAILABLE`. Success returns `200` with the shared `ShortUrlResponse`; invalid
input returns `400 VALIDATION_ERROR`.

Aliases are immutable. Domain assignment is mutable but ownership is not: a composite database
foreign key prevents a link from referencing another owner's claim. The route short code, `id`,
`ownerId`, `createdAtUtc`, deletion state, active state, and counters are not update-body fields.
An alias remains globally claimed while its row is soft-deleted, so creation with the same alias
returns `409 ALIAS_CONFLICT`.

## Active state

`PATCH /api/v1/short-urls/{shortCode}/status` requires an explicit Boolean:

```json
{
  "isActive": false
}
```

Omitting `isActive` returns `400 VALIDATION_ERROR`. A deactivated link remains manageable but `GET /r/{shortCode}` returns `404 NOT_FOUND`. Reactivation restores redirect eligibility unless the link is expired or deleted.

## Soft delete and restore

`DELETE /api/v1/short-urls/{shortCode}` records `deletedAtUtc`, returns `204`, and excludes the link from normal owner lists, detail/stats operations, and public redirect resolution. Add `includeDeleted=true` to the owner list to find deleted links; deleted list items expose both `deletedAtUtc` and `restoreUntilUtc`. Deletion preserves the link's active state, alias claim, click history, and other fields.

`POST /api/v1/short-urls/{shortCode}/restore` restores the owned row and returns `200` with its details. The default restore window is 30 days and is configured by `ShortUrlLifecycle:SoftDeleteRetentionDays` (valid range 1–3,650). `restoreUntilUtc` is the exclusive boundary: restore is permitted only while application UTC now is earlier than that instant. At or after the boundary, restore returns `410 RESTORE_WINDOW_EXPIRED`, even if a later cleanup job has not physically removed the row. A row with no trustworthy deletion timestamp is also non-restorable. Restoring a non-deleted row returns `409 RESTORE_NOT_DELETED`.

Restore preserves the pre-delete active state. Consequently, a restored inactive link still returns `404` from redirect until activated, and a restored expired link returns `410 EXPIRED` until its expiry is replaced or cleared.

## Cache and concurrency semantics

Destination/expiry/domain updates, status changes, soft deletion, and restore remove the
host/code identity through `IShortUrlCache` after the database save succeeds. A domain move
invalidates both old and new host identities. Domain disable or verification restart invalidates
every assigned link; persisted route guards remain authoritative if removal fails or races.

No alias mutation or ownership transfer occurs in this lifecycle, so no new uniqueness race is introduced. Mutations load an owner-scoped row and save only changed scalar properties. Competing writes to the same property use database last-commit-wins behavior; writes to different properties can both survive. Delete/update races retain deletion because the update does not write deletion fields. Cache invalidation happens after every successful redirect-affecting commit.

## Status summary

| Endpoint | Success | Other documented outcomes |
| --- | --- | --- |
| `PUT /api/v1/short-urls/{shortCode}` | `200` details | `400`, `401`, `403`, `404` |
| `PATCH /api/v1/short-urls/{shortCode}/status` | `200` details | `400`, `401`, `403`, `404` |
| `DELETE /api/v1/short-urls/{shortCode}` | `204` | `401`, `403`, `404` |
| `POST /api/v1/short-urls/{shortCode}/restore` | `200` details | `401`, `403`, `404`, `409`, `410` |

The OpenAPI document includes these request/response schemas, response codes, and lifecycle descriptions.
