# TASK-012 — Concurrency-Safe Owned URL Creation

**Status:** Completed  
**Phase:** 03 — Owned URL Lifecycle & Management API

## Goal

Make short-link creation correct under concurrent requests while preserving custom aliases, expiry, ownership, validation, and deterministic error behavior.

## Dependencies

- Phase 02 completed.

## Scope

- Refactor generated-code creation so database uniqueness is authoritative.
- Handle generated-code collisions through bounded retry after insert/unique-conflict detection rather than relying only on check-then-insert.
- Map custom-alias uniqueness conflicts to the documented API conflict response.
- Derive owner from authenticated context.
- Validate destination URL, alias, and expiry through application/API validation boundaries.
- Review short-code length/charset and document the chosen entropy/collision strategy.

## Acceptance Criteria

- [x] Database unique constraint remains the final authority for short-code uniqueness.
- [x] Generated short-code collisions are retried with a bounded attempt policy.
- [x] Custom-alias conflicts return the documented conflict response and are not retried as generated-code collisions.
- [x] Ownership is derived from the authenticated user, not request input.
- [x] Invalid schemes and malformed/oversized URLs are rejected consistently.
- [x] Expiry must follow the documented UTC/future-date policy.
- [x] Failed creation does not leave partial records.
- [x] OpenAPI reflects the current creation contract.
- [x] Build succeeds and collision behavior is manually exercised where practical.
- [x] Automated test files remain deferred to Phase 16.

## Verification

Document normal creation, custom alias creation, duplicate alias, and a reproducible/manual collision-path verification method.

## Implementation and Verification Record

Completed on 2026-08-11.

- Replaced application-level existence checks with repository `TryCreateAsync` semantics. SQL Server saves the candidate and translates only duplicate-key errors 2601/2627 naming `IX_ShortUrls_ShortCode`; it detaches a rejected candidate so the scoped context is safe for retry. The in-memory repository performs the equivalent create-or-conflict operation atomically under its lock.
- A custom alias receives one insert attempt and maps a short-code conflict to `AliasConflictException`. Generated creation makes at most five authoritative insert attempts. The service derives `OwnerId` from `ICurrentUserContext`, creates a fresh entity for each candidate, and fills cache only after persistence succeeds.
- Increased generated codes from six to eight uniformly sampled base-62 characters: `62^8` possibilities (about 47.6 bits). The full strategy and deterministic reproduction steps are documented in `docs/url-creation.md`.
- Added application validation for the existing 2,048-character database URL limit, absolute HTTP/HTTPS schemes, and future UTC (`Z`) expiry. The validator uses `IDateTimeProvider` rather than the ambient system clock.
- A temporary, ignored manual harness seeded a collision and returned `COLLIDE1` then `SUCCESS1`; creation retried to `SUCCESS1` without changing the seeded row. Five repeated collisions produced `ShortCodeGenerationFailedException` without a partial row. It also ran 24 simultaneous custom-alias requests against the in-memory repository and observed exactly one creation plus 23 conflicts. The harness was removed after verification; no automated test files were added.
- SQL-backed API verification returned `201` for normal generated creation (eight-character code), `201` for custom-alias creation, and `409 ALIAS_CONFLICT` for its duplicate. SQL diagnostics showed the rejected insert naming `IX_ShortUrls_ShortCode`. Generated OpenAPI advertised Bearer security and the documented creation responses; the final contract includes `201`, `400`, `401`, `409`, `429`, and `500`.
- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. `dotnet format UrlShortener.sln --verify-no-changes --no-restore` completed successfully. `dotnet-ef migrations has-pending-model-changes --project UrlShortener.Infrastructure --startup-project UrlShortener.Api --no-build` reported no pending model changes.
