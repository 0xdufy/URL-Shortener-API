# TASK-009 — Registration, Sign-In, Session, and Sign-Out API

**Status:** Planned  
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

- [ ] A new valid user can authenticate using the documented flow.
- [ ] Invalid credentials do not reveal whether a specific account exists beyond the approved error policy.
- [ ] Protected current-user/session endpoint returns only safe profile/session metadata.
- [ ] Expired or revoked credentials are rejected according to the ADR.
- [ ] Sign-out prevents continued use of credentials that are defined as revocable by the selected architecture.
- [ ] Authentication endpoints use the common API error contract.
- [ ] Sensitive credentials never appear in logs or error bodies.
- [ ] OpenAPI accurately describes request/response/status contracts.
- [ ] Build succeeds and flows are manually verified without adding automated test files.

## Verification

Use documented HTTP requests to exercise successful registration/sign-in/session/sign-out plus invalid credential and expired/revoked scenarios. Record commands/results in completion notes.