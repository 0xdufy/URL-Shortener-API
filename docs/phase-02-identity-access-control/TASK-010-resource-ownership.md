# TASK-010 — User Ownership Model for Managed Resources

**Status:** Completed  
**Phase:** 02 — Identity & Access Control

## Goal

Introduce explicit ownership so every protected short URL and future user-owned resource can be authorized server-side.

## Dependencies

- TASK-009 completed.

## Scope

- Add owner identity to `ShortUrl` or the approved equivalent.
- Define how existing pre-authentication rows are migrated: assigned, marked legacy/anonymous, or otherwise handled by an explicit migration rule.
- Introduce a current-user/access-context abstraction usable by application use cases without coupling core logic to controller internals.
- Define ownership rules for future API keys and custom domains.
- Preserve public redirect behavior: ownership must not be required to follow a valid public short URL.

## Acceptance Criteria

- [x] Every newly created authenticated short URL stores an immutable owner relationship.
- [x] Database foreign keys/indexes required for efficient owner-scoped queries exist.
- [x] Legacy rows have a documented deterministic migration treatment.
- [x] Application code can request current authenticated user identity through one clear abstraction.
- [x] Redirect resolution remains public for valid links.
- [x] Ownership identifiers are not accepted from client request bodies when they can be derived from authenticated context.
- [x] Owner identity cannot be changed through normal URL update operations.
- [x] Build and migration application succeed.
- [x] No automated test files are added.

## Verification

Create at least two users and owned links manually. Inspect persistence to confirm ownership is derived from authentication context rather than caller-supplied identifiers.

## Implementation and Verification Record

Completed on 2026-08-11.

- Added immutable `ShortUrl.OwnerId` construction, a restrictive user foreign key, and the `(OwnerId, CreatedAtUtc)` index in migration `20260811183558_AddShortUrlOwnership`.
- Added the application-layer `ICurrentUserContext` boundary and the HTTP JWT-subject adapter. Authenticated creation derives ownership from that context; the request DTO has no owner field.
- Classified existing rows deterministically as legacy/unowned (`OwnerId IS NULL`) and documented future API-key/custom-domain ownership in `docs/resource-ownership.md`.
- Applied all migrations successfully to Development LocalDB. A two-user manual check created link `affcf1e140` for user `39254c8a-9b39-4c05-b737-95f27d956a28` and link `ba76660e07` for user `5c93bf5a-ee2a-4601-8fae-d253169223be`. Each create body deliberately supplied the other user's `ownerId`; direct database inspection confirmed the persisted owners still matched the authenticated JWT subjects.
- Confirmed unauthenticated creation returns `401 AUTHENTICATION_REQUIRED`, while unauthenticated `GET /r/affcf1e140` returns `302` to the public destination.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. `dotnet format UrlShortener.sln --verify-no-changes --no-restore` completed successfully. No automated test files were added.
