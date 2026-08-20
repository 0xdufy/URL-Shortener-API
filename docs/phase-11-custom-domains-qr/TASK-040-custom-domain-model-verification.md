# TASK-040 — Custom Domain Model and Ownership Verification

**Status:** Completed
**Phase:** 11 — Custom Domains & QR Codes

## Goal

Allow users to register custom domains only after an explicit ownership-verification workflow, without trusting a hostname merely because a user submitted it.

## Dependencies

- Phase 10 completed.

## Scope

- Add user-owned custom-domain entity with normalized host, status, verification token/method, timestamps, and optional failure metadata.
- Enforce normalized hostname uniqueness according to the product policy.
- Design DNS-based verification as the preferred approach unless an ADR approves another method.
- Implement request-verification/check-verification API operations.
- Define pending, verified, failed/invalid, and disabled states.
- Prevent protected/reserved platform hosts from being claimed.

## Acceptance Criteria

- [x] Domain ownership is immutable to the authenticated owner through normal update flows.
- [x] Hostnames are normalized consistently before uniqueness checks.
- [x] Database constraints prevent duplicate conflicting domain claims according to policy.
- [x] Verification requires evidence external to the application account itself.
- [x] Verification tokens are high-entropy and scoped to the domain record.
- [x] Unverified/disabled domains cannot be used to generate active branded short URLs.
- [x] Verification errors are actionable without exposing internal resolver details unnecessarily.
- [x] Reserved platform hosts cannot be registered as user domains.
- [x] Migrations/backend build and manual verification flow succeed.
- [x] No automated test files are added.

## Verification

Exercise pending and failed verification using a controlled domain or documented local/stub workflow, and confirm unverified domains cannot be activated for URL generation.

## Implementation and Verification Notes

- 2026-08-19: Added an owner-scoped `CustomDomain` aggregate with immutable owner/normalized-host
  identity, DNS TXT method/current token, row version, pending/verified/failed/disabled states,
  audit timestamps, safe optional failure metadata, and a single `CanServeBrandedLinks` eligibility
  rule that is true only while verified.
- Registration performs lowercase IDNA ASCII normalization before policy and persistence checks.
  SQL Server uses a binary-collated normalized-host column and global unique index; check constraints
  bound the state/method and required state metadata. The canonical public host plus configured
  reserved namespaces and their parent/child namespace relationships cannot be claimed.
- Added owner-filtered register/list/request/check/disable operations under
  `/api/v1/custom-domains`. Request rotates a 256-bit `RandomNumberGenerator` base64url token.
  Check requires an exact external DNS TXT value through a bounded, timeout-controlled
  DNS-over-HTTPS client. Missing, stale, and unavailable DNS results become actionable stable
  failure metadata without exposing resolver response bodies or exception details.
- Migration `20260820190104_AddCustomDomainVerificationModel` and the complete preceding migration
  chain applied successfully to a fresh isolated LocalDB database. The idempotent SQL script
  contained the intended constraints/indexes, EF reported no pending model changes, and the
  isolated database was removed after verification.
- A temporary uncommitted stub-verifier harness confirmed IDNA normalization, global duplicate
  rejection, owner isolation, reserved namespace rejection, 43-character token entropy/rotation,
  and `pending -> failed -> pending (new token) -> verified -> disabled`. Eligibility remained
  false except in `verified`. The harness was removed, and no automated test file was added.
- A running Development API exposed all five intended OpenAPI operations (register, list, request,
  check, disable). Targeted formatting verification and `dotnet build UrlShortener.sln --no-restore`
  succeeded; the final solution build completed with zero warnings and zero errors. The documented
  loopback DNS JSON workflow covers controlled end-to-end resolver checks without public DNS.
