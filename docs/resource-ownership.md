# Resource Ownership

## Short URL ownership

Every short URL created through `POST /api/v1/short-urls` is owned by the authenticated user. The endpoint requires a bearer access token, and `ShortUrlService` obtains the stable user ID from `ICurrentUserContext`. `OwnerId` is not part of the request DTO and caller-supplied JSON fields cannot select or transfer ownership.

`ShortUrl.OwnerId` has a private setter. Normal create and update use cases therefore cannot replace it after construction. Any future ownership transfer must be an explicit privileged use case with its own authorization, audit, and concurrency rules; it must not reuse an ordinary URL update contract.

The database enforces a nullable foreign key from `ShortUrls.OwnerId` to `Users.Id` with restrictive delete behavior. The `(OwnerId, CreatedAtUtc)` index supports deterministic owner-scoped queries. Account deletion must resolve owned resources explicitly rather than cascading public links away.

## Legacy-row migration rule

Migration `20260811183558_AddShortUrlOwnership` adds `OwnerId` as nullable. All rows created before ownership existed deterministically remain `NULL`; no account is guessed from URL or access-log data. These rows are classified as legacy/unowned:

- Public redirect resolution continues to serve a valid legacy link.
- Legacy rows cannot be claimed or managed through an ordinary client request.
- TASK-011 owner-scoped management queries must exclude them.
- A future administrator migration may assign or retire them only through an explicit, audited operational procedure.

The nullable column makes the migration safe for populated databases while application construction requires a non-empty owner for every new link. Rolling back removes only the ownership FK, index, and column; it does not delete users or short URLs.

## Current-user boundary

Application use cases depend on `ICurrentUserContext`, which exposes only the provider-neutral `Guid? UserId`. `HttpCurrentUserContext` is the API adapter that reads the validated JWT subject claim. Domain and Application code do not depend on `HttpContext`, `ClaimsPrincipal`, or ASP.NET Core Identity.

The application layer still rejects creation if no valid user ID is available. This preserves the invariant when a use case is invoked outside MVC or an authorization attribute is accidentally changed.

## Future owned resources

API keys and custom domains follow the same rules:

- An API key belongs immutably to the authenticated user who creates it. Its owner is derived from access context, never from the create body. Using a key grants only its documented scopes over resources belonging to that owner; it does not impersonate another owner.
- A custom domain belongs immutably to the authenticated user who starts verification. Verification state does not change ownership. Domain uniqueness is global, management queries are owner-scoped, and deletion or transfer requires an explicit lifecycle operation.
- Both schemas require owner foreign keys and indexes beginning with `OwnerId`. User deletion uses restrictive behavior until the owning feature defines an explicit revoke/unlink/retention workflow.

Public redirect resolution remains ownership-independent: a valid active short code can be followed without authentication, regardless of whether the row is owned or legacy.
