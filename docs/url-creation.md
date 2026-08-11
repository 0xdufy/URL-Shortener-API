# URL Creation Contract

`POST /api/v1/short-urls` creates a link for the authenticated user. The request cannot supply an owner ID; the application obtains the stable user ID from `ICurrentUserContext` and rejects creation when it is absent.

## Input rules

- `originalUrl` is required, may contain at most 2,048 characters, and must be an absolute `http` or `https` URI.
- `customAlias` may be omitted, `null`, or whitespace-only. A nonblank alias must contain 4–20 ASCII letters, digits, underscores, or hyphens (`^[A-Za-z0-9_-]+$`). Codes are case-sensitive.
- `expiresAtUtc` may be omitted or `null`. When supplied, it must be a future UTC timestamp serialized with the `Z` designator, for example `2026-12-31T00:00:00Z`. Offset, local, and unspecified timestamps are rejected rather than silently reinterpreted.

The application validates these bounds before persistence. SQL Server independently limits `OriginalUrl` to 2,048 characters and `ShortCode` to 20 characters.

## Uniqueness and collision handling

The unique SQL Server index `IX_ShortUrls_ShortCode` is the final authority. Creation does not use an existence pre-check. The repository adds and saves one candidate, then reports either `Created` or `ShortCodeConflict`. It classifies only SQL Server duplicate-key errors 2601/2627 that identify this index; unrelated persistence failures remain errors. A failed SQL insert is atomic, its tracked candidate is detached before another attempt, and the cache is populated only after a successful save. The Development-only in-memory repository makes the same create-or-conflict decision inside one lock.

A custom alias gets one persistence attempt. A conflict is not retried and returns `409 ALIAS_CONFLICT`, regardless of whether the existing code was originally generated or custom. Soft deletion does not release a code.

Generated codes use eight uniformly sampled characters from `A-Z`, `a-z`, and `0-9`, backed by `RandomNumberGenerator.GetInt32`. This provides `62^8` (218,340,105,584,896) possible values, approximately 47.6 bits of entropy. At one million existing codes, one new candidate has roughly a 1-in-218-million collision probability. Creation makes at most five insert attempts; five conflicts return `500 SHORTCODE_GENERATION_FAILED` without leaving a candidate row or cache entry.

## Reproducing the collision path

For a deterministic manual check, substitute `IShortCodeGenerator` in a local harness with a sequence generator:

1. Persist an existing row with code `COLLIDE1`.
2. Return `COLLIDE1` and then `SUCCESS1` from the generator.
3. Create a generated link and confirm the response uses `SUCCESS1` while the original `COLLIDE1` row is unchanged.
4. Return `COLLIDE1` for all five calls and confirm `ShortCodeGenerationFailedException` is raised and the original row remains unchanged.
5. Submit the same valid custom alias twice against SQL Server. Confirm the first call returns `201` and the second returns `409 ALIAS_CONFLICT`; SQL diagnostics should identify `IX_ShortUrls_ShortCode`.

Automated unit and integration test files for these scenarios remain scheduled for Phase 16.
