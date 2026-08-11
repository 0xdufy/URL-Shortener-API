# TASK-040 — Custom Domain Model and Ownership Verification

**Status:** Planned  
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

- [ ] Domain ownership is immutable to the authenticated owner through normal update flows.
- [ ] Hostnames are normalized consistently before uniqueness checks.
- [ ] Database constraints prevent duplicate conflicting domain claims according to policy.
- [ ] Verification requires evidence external to the application account itself.
- [ ] Verification tokens are high-entropy and scoped to the domain record.
- [ ] Unverified/disabled domains cannot be used to generate active branded short URLs.
- [ ] Verification errors are actionable without exposing internal resolver details unnecessarily.
- [ ] Reserved platform hosts cannot be registered as user domains.
- [ ] Migrations/backend build and manual verification flow succeed.
- [ ] No automated test files are added.

## Verification

Exercise pending and failed verification using a controlled domain or documented local/stub workflow, and confirm unverified domains cannot be activated for URL generation.