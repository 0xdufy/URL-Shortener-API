# TASK-008 — Authentication Architecture and Identity Data Model

**Status:** Completed
**Phase:** 02 — Identity & Access Control

## Goal

Choose and document the authentication/session strategy before writing endpoint logic, and introduce the persistence/domain primitives required for secure user ownership.

## Dependencies

- Phase 01 completed.

## Scope

- Evaluate an ASP.NET Core Identity-based implementation versus a deliberately smaller custom identity model; choose one and record the ADR.
- Define user identity, normalized email/username policy if applicable, account status, creation/update timestamps, and security-sensitive fields.
- Define session/token strategy, expiration, revocation, rotation, storage, and transport.
- Define password policy and password-hashing ownership.
- Define authentication error semantics without account-enumeration leakage where practical.
- Add required schema/migrations, but do not yet build the Angular UI.

## Security Requirements

- Never store plaintext passwords, refresh tokens, session secrets, or recoverable API secrets.
- Authentication secrets must never be logged.
- Cookie-based strategies must define Secure/HttpOnly/SameSite and CSRF implications.
- Bearer-token strategies must define refresh/revocation and browser-storage implications.

## Acceptance Criteria

- [x] ADR documents the selected auth architecture and rejected alternatives.
- [x] User/identity schema supports unique identity constraints at the database layer.
- [x] Password hashing uses framework/provider-approved cryptography rather than custom hashing.
- [x] Session/token lifetime and revocation behavior are explicitly documented.
- [x] Required migrations are created and can be applied to a clean database.
- [x] No plaintext credentials/secrets are persisted.
- [x] Logging guidance covers all identity secrets.
- [x] Domain/application code does not depend directly on HTTP-specific identity objects where a boundary abstraction is appropriate.
- [x] No automated test files are added.

## Verification

Apply migrations to a clean local database and start the application. Inspect the resulting schema and confirm all uniqueness and nullability requirements match the ADR.

## Completion Notes

- 2026-08-11: Accepted ADR 0003. Selected ASP.NET Core Identity accounts, short-lived signed JWT access tokens, and rotating database-backed refresh sessions whose 256-bit bearer secrets are persisted only as SHA-256 hashes.
- Added the provider-neutral account-status primitive, Infrastructure-owned Identity user/session models, validated password/lockout/session-lifetime settings, EF Identity stores, and migration `20260811180034_AddIdentityAndRefreshSessions`.
- Applied the full migration chain to the clean disposable LocalDB database `UrlShortenerPhase02Verification`. Schema inspection confirmed non-null normalized identity/password-hash/security fields, unique normalized email and username indexes, a unique non-null `binary(32)` refresh-token hash, expiry checks, row-version concurrency, and user/session foreign keys.
- Started the SQL-backed API at `http://127.0.0.1:5098`; `GET /swagger/index.html` returned `200`. `dotnet format UrlShortener.sln --verify-no-changes --no-restore` passed, and `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors. No automated test files were added.
