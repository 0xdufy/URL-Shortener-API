# TASK-009 — Registration, Sign-In, Session, and Sign-Out API

**Status:** Completed
**Phase:** 02 — Identity & Access Control

## Goal

Implement the authentication flows defined by TASK-008 as stable API contracts suitable for both Angular and programmatic clients.

## Dependencies

- TASK-008 completed.

## Scope

Implement the ADR-approved equivalents of:

- Registration when public registration is enabled.
- Sign-in.
- Current-session/current-user retrieval.
- Session/token refresh when required by the selected strategy.
- Sign-out/revocation.
- Consistent authentication error responses.
- Authentication-specific rate-limit hooks ready for the later distributed limiter phase.

## Requirements

- Normalize and validate identity inputs consistently.
- Do not expose password hashes, token hashes, security stamps, internal provider identifiers, or other secret material.
- Make client-visible expiration/session behavior explicit.
- Treat logout/revocation as a server-side security operation where the chosen auth model supports it.

## Acceptance Criteria

- [x] A new valid user can authenticate using the documented flow.
- [x] Invalid credentials do not reveal whether a specific account exists beyond the approved error policy.
- [x] Protected current-user/session endpoint returns only safe profile/session metadata.
- [x] Expired or revoked credentials are rejected according to the ADR.
- [x] Sign-out prevents continued use of credentials that are defined as revocable by the selected architecture.
- [x] Authentication endpoints use the common API error contract.
- [x] Sensitive credentials never appear in logs or error bodies.
- [x] OpenAPI accurately describes request/response/status contracts.
- [x] Build succeeds and flows are manually verified without adding automated test files.

## Verification

Use documented HTTP requests to exercise successful registration/sign-in/session/sign-out plus invalid credential and expired/revoked scenarios. Record commands/results in completion notes.

## Completion Notes

- 2026-08-11: Added provider-neutral Application authentication contracts/validators and SQL-backed Infrastructure use cases for registration, email/password sign-in, session inspection, rotating refresh, reuse detection, and family revocation. ASP.NET Core Identity remains the sole password-hashing owner.
- Added `POST /api/v1/auth/register`, `POST /api/v1/auth/sign-in`, `GET /api/v1/auth/me`, `POST /api/v1/auth/refresh`, and `POST /api/v1/auth/sign-out`. JWT bearer challenges, endpoint exceptions, validation, CSRF failures, rate limits, and unavailable identity persistence use the lowercase common error envelope.
- Added validated issuer/audience/lifetime/signing-key, trusted-origin, secure-cookie, registration, and authentication rate-limit configuration. SQL mode requires a secret-supplied base64 key of at least 32 bytes; Development in-memory mode starts normally but returns controlled `503 AUTHENTICATION_UNAVAILABLE` responses for auth operations.
- Added migration `20260811182009_AddRefreshSessionSecurityStampHash`, applied all migrations to clean LocalDB database `UrlShortenerPhase02Task009Verification`, and confirmed 32-byte refresh/security-stamp hashes. The verification database held one framework password hash of length 84; no plaintext password or refresh-token field exists.
- Manual HTTP verification results: registration `201`; sign-in `200`; authenticated `/me` `200`; missing bearer token `401 AUTHENTICATION_REQUIRED`; unknown and wrong-password sign-in both `401 AUTHENTICATION_FAILED` with the same message; missing CSRF inputs `400 CSRF_VALIDATION_FAILED`; refresh `200`; sign-out `204`; refresh after sign-out `401 INVALID_SESSION`; rotated-token reuse `401 INVALID_SESSION`; its replacement was then rejected with `401`; and a database-expired refresh session returned `401`.
- OpenAPI exposed all five auth paths and the bearer scheme, marked `/me` as secured without marking public sign-in as secured, and documented the declared status responses. The refresh token was HttpOnly and absent from JSON. A log scan found neither the manual password nor JWT-shaped values.
- `dotnet format UrlShortener.sln --verify-no-changes --no-restore` passed. `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors. No automated test files were added, per the Phase 16 deferral.
